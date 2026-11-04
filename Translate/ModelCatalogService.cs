namespace Translate;

/// <summary>Reads the selected account's catalog without making a generation request.</summary>
public sealed class ModelCatalogService
{
    private static readonly HttpClient SharedClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient _client;
    public ModelCatalogService(HttpClient? client = null) => _client = client ?? SharedClient;

    public static Uri BuildEndpoint(string address)
    {
        var chat = TranslationService.BuildEndpoint(address).AbsoluteUri;
        return new Uri(chat[..^"/chat/completions".Length] + "/models");
    }

    public async Task<IReadOnlyList<string>> ListAsync(AppSettings settings, CancellationToken token = default)
        => (await ListDetailsAsync(settings, token)).Select(model => model.Id).ToArray();

    public async Task<IReadOnlyList<TranslationModelInfo>> ListDetailsAsync(AppSettings settings, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(settings.TranslationApiKey)) throw new InvalidOperationException("请先填写 API Key，再获取模型列表。" );
        var endpoint = BuildEndpoint(settings.TranslationBaseUrl);
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.TranslationApiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            using var response = await _client.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var hint = (int)response.StatusCode switch
                {
                    401 or 403 => "请检查 API Key 及模型列表访问权限。",
                    404 or 405 => "服务未提供兼容的 /models 接口，请检查 Base URL。",
                    429 => "请求过于频繁或额度不足，请稍后重试。",
                    _ => "请检查服务状态后重试。"
                };
                throw new InvalidOperationException($"获取模型列表失败（HTTP {(int)response.StatusCode}）。{hint}");
            }
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("模型列表格式不兼容，服务应返回 data 数组。" );
            var officialOpenAi = endpoint.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase);
            return data.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                .Select(item => new TranslationModelInfo(item.GetProperty("id").GetString()!, ReadEffortLevels(item)))
                .Where(model => !string.IsNullOrWhiteSpace(model.Id) && (!officialOpenAi || IsTextChatCandidate(model.Id)))
                .DistinctBy(model => model.Id, StringComparer.Ordinal)
                .OrderBy(model => model.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new TimeoutException("获取模型列表超时，请检查网络后点击刷新。" ); }
        catch (JsonException ex) { throw new InvalidOperationException("服务返回的模型列表不是有效 JSON。", ex); }
        catch (HttpRequestException ex) { throw new InvalidOperationException("无法连接模型列表接口，请检查网络和 API 地址。", ex); }
    }

    private static IReadOnlyList<string> ReadEffortLevels(JsonElement model)
    {
        if (model.TryGetProperty("effort", out var effort) && effort.ValueKind == JsonValueKind.Object
            && effort.TryGetProperty("supported_levels", out var levels) && levels.ValueKind == JsonValueKind.Array)
            return levels.EnumerateArray().Where(level => level.ValueKind == JsonValueKind.String).Select(level => level.GetString()!).ToArray();
        return [];
    }

    // /models has no endpoint-capability field. Hide clearly unsuitable families, but a
    // successful chat test is still required to establish access and protocol compatibility.
    internal static bool IsTextChatCandidate(string id)
    {
        var name = id.ToLowerInvariant();
        if (name.StartsWith("ft:", StringComparison.Ordinal)) name = name[3..].Split(':')[0];
        // These documented models require Responses, which is not this adapter's protocol.
        if (name.StartsWith("gpt-5.2-pro", StringComparison.Ordinal) || name.StartsWith("gpt-5.4-pro", StringComparison.Ordinal)) return false;
        if (!(name.StartsWith("gpt-", StringComparison.Ordinal) || name.StartsWith("chatgpt-", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(name, "^o[0-9]+(?:-|$)"))) return false;
        return !new[] { "image", "audio", "realtime", "transcribe", "tts", "instruct", "codex", "deep-research", "search" }.Any(name.Contains)
            && name is not "o1-preview" and not "o1-mini";
    }
}
