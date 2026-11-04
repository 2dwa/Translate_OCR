namespace MiOcrDesk;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string source, AppSettings settings, Action<string> progress, CancellationToken token = default);
}

/// <summary>Provider boundary: additional native protocols can implement ITranslationService.</summary>
public sealed class TranslationService : ITranslationService
{
    private static readonly HttpClient SharedClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient _client;
    public TranslationService(HttpClient? client = null) => _client = client ?? SharedClient;

    public static Uri BuildEndpoint(string address)
    {
        if (!Uri.TryCreate(address.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("请输入有效的 HTTPS 接口地址，不要包含密钥、查询参数或 # 片段。" );
        var url = uri.AbsoluteUri.TrimEnd('/');
        return new Uri(url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ? url : url + "/chat/completions");
    }

    public async Task<TranslationResult> TranslateAsync(string source, AppSettings settings, Action<string> progress, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("请先识别或输入需要翻译的文字。" );
        if (string.IsNullOrWhiteSpace(settings.TranslationApiKey)) throw new InvalidOperationException("请先在偏好设置中填写翻译 API Key。" );
        if (string.IsNullOrWhiteSpace(settings.TranslationModel)) throw new InvalidOperationException("请先在偏好设置中选择翻译模型。" );
        var uri = BuildEndpoint(settings.TranslationBaseUrl);
        var prompt = settings.TranslationPrompt.Replace("{targetLanguage}", settings.TargetLanguage, StringComparison.Ordinal);
        var payload = new Dictionary<string, object>
        {
            ["model"] = settings.TranslationModel,
            ["messages"] = new[] { new { role = "system", content = prompt }, new { role = "user", content = source } },
            ["stream"] = false
        };
        ReasoningOptions.Apply(payload, settings);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.TranslationApiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TranslationTimeoutSeconds, 15, 600)));
        progress($"正在通过 {settings.TranslationProvider} 翻译为{settings.TargetLanguage}…");
        try
        {
            using var response = await _client.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var hint = (int)response.StatusCode switch { 400 => "请检查模型及推理强度是否兼容，可先选择「模型默认」重试。", 401 or 403 => "请检查 API Key 和模型访问权限。", 402 => "服务商余额不足，请检查账户余额。", 404 => "请检查接口地址和模型名称。", 429 => "请求过于频繁或额度不足，请稍后重试。", _ => "请检查服务状态后重试。" };
                throw new InvalidOperationException($"翻译服务返回 {(int)response.StatusCode}。{hint}");
            }
            progress("正在整理翻译结果…");
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
                throw new InvalidOperationException("服务响应格式不兼容，请选择支持 Chat Completions 的接口。" );
            var choice = choices[0];
            if (choice.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("服务未返回有效的翻译结果。" );
            if (choice.TryGetProperty("finish_reason", out var finish) && finish.GetString() == "length") throw new InvalidOperationException("译文超过模型输出上限，请分段翻译。" );
            if (!choice.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object || !message.TryGetProperty("content", out var text) || text.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("服务未返回文本，请检查模型配置或内容限制。" );
            var content = text.GetString();
            if (string.IsNullOrWhiteSpace(content)) throw new InvalidOperationException("翻译服务未返回文本，请检查模型配置。" );
            return new TranslationResult(content.Trim(), root.TryGetProperty("model", out var model) && model.ValueKind == JsonValueKind.String ? model.GetString() ?? settings.TranslationModel : settings.TranslationModel);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new TimeoutException("翻译请求超时，可在设置中增加等待时间后重试。" ); }
        catch (JsonException ex)
        { throw new InvalidOperationException("服务响应格式不兼容，请选择支持 Chat Completions 的接口。", ex); }
    }
}
