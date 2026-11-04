using System.Diagnostics;

namespace MiOcrDesk;

public sealed record RuntimeState(bool Ready, bool Installed, string Root, string Detail);

/// <summary>Uses the bundled Ollama runtime directly; no machine-specific Python environment.</summary>
public sealed class OcrRuntimeService
{
    public const string ModelName = "longwayxu/xiaomi-ocr-0:bf16";
    public const string ServiceUrl = "http://127.0.0.1:11435";
    private static readonly HttpClient SharedClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient Client;
    private Process? _ownedProcess;
    private static readonly SemaphoreSlim StartupGate = new(1, 1);
    public OcrRuntimeService(HttpClient? client = null) => Client = client ?? SharedClient;
    internal OcrRuntimeService(HttpClient client, Process ownedProcess) : this(client) => _ownedProcess = ownedProcess;

    public static string ResolveRoot(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.RuntimeRoot)) return Path.GetFullPath(settings.RuntimeRoot);
        var configured = Environment.GetEnvironmentVariable("MIOCR_RUNTIME_ROOT");
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
        var bundled = Path.Combine(AppContext.BaseDirectory, "OcrRuntime");
        if (Directory.Exists(bundled)) return bundled;
        // F5 and direct execution from bin/ both discover the development deployment.
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var projectBundle = Path.Combine(directory.FullName, "OcrRuntime");
            if (File.Exists(Path.Combine(projectBundle, "runtime", "ollama.exe")) && Directory.Exists(Path.Combine(projectBundle, "models"))) return projectBundle;
            if (File.Exists(Path.Combine(directory.FullName, "runtime", "ollama.exe")) && Directory.Exists(Path.Combine(directory.FullName, "models"))) return directory.FullName;
        }
        return bundled;
    }

    public static string? ValidateBundle(string root)
    {
        if (!File.Exists(Path.Combine(root, "runtime", "ollama.exe"))) return "未找到运行时 runtime/ollama.exe。请选择完整离线包目录。";
        var manifest = Path.Combine(root, "models", "manifests", "registry.ollama.ai", "longwayxu", "xiaomi-ocr-0", "bf16");
        if (!File.Exists(manifest)) return "未找到 Xiaomi OCR 模型清单，请使用包含模型的完整安装包。";
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
            var parts = doc.RootElement.GetProperty("layers").EnumerateArray().ToList();
            parts.Add(doc.RootElement.GetProperty("config"));
            foreach (var part in parts)
            {
                var digest = part.GetProperty("digest").GetString() ?? "";
                if (!System.Text.RegularExpressions.Regex.IsMatch(digest, "^sha256:[a-f0-9]{64}$")) return "模型清单格式无效。";
                var file = new FileInfo(Path.Combine(root, "models", "blobs", digest.Replace(':', '-')));
                if (!file.Exists || file.Length != part.GetProperty("size").GetInt64()) return "模型文件缺失或不完整，请重新复制完整离线包。";
            }
            return null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException or KeyNotFoundException)
        { return "无法读取模型清单，请重新选择完整离线包。"; }
    }

    public async Task<RuntimeState> InspectAsync(AppSettings settings, CancellationToken token = default)
    {
        var root = ResolveRoot(settings);
        var error = ValidateBundle(root);
        var ready = await IsReadyAsync(token);
        return new RuntimeState(ready, error is null, root, ready
            ? "小米 OCR 服务已连接。截图在本机处理，首次识别时会加载模型。"
            : error ?? "运行时与模型文件完整。点击「检测并启动」，或在首次识别时自动启动。" );
    }

    public async Task EnsureRuntimeAsync(AppSettings settings, Action<string> progress, CancellationToken token)
    {
        await StartupGate.WaitAsync(token);
        try
        {
            if (await IsReadyAsync(token)) return;
            // A cancelled or failed previous startup may have left a nonresponsive owned daemon.
            TerminateOwnedProcess();
            var root = ResolveRoot(settings);
            if (ValidateBundle(root) is { } error) throw new InvalidOperationException(error);
            progress("正在启动本地识别引擎…");
            var start = new ProcessStartInfo(Path.Combine(root, "runtime", "ollama.exe"), "serve")
            {
                WorkingDirectory = Path.Combine(root, "runtime"), UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardError = true, RedirectStandardOutput = true
            };
            start.Environment["OLLAMA_HOST"] = "127.0.0.1:11435";
            start.Environment["OLLAMA_MODELS"] = Path.Combine(root, "models");
            start.Environment["OLLAMA_NO_CLOUD"] = "1";
            var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动本地识别引擎。");
            _ownedProcess?.Dispose(); _ownedProcess = process;
            // Drain both pipes for the lifetime of the daemon. Keep only a bounded error tail.
            var tail = new Queue<string>();
            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (tail) { tail.Enqueue(e.Data); while (tail.Count > 8) tail.Dequeue(); } };
            process.BeginOutputReadLine(); process.BeginErrorReadLine();
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < TimeSpan.FromSeconds(60))
            {
                token.ThrowIfCancellationRequested();
                if (await IsReadyAsync(token)) return;
                if (process.HasExited)
                {
                    string details;
                    lock (tail) details = string.Join(" ", tail.TakeLast(2));
                    throw new InvalidOperationException("本地引擎未能启动。请检查端口 11435 是否被占用。" + details);
                }
                await Task.Delay(500, token);
            }
            throw new TimeoutException("本地引擎启动超时，请在「本地模型」中检查安装目录后重试。");
        }
        finally { StartupGate.Release(); }
    }

    public async Task<OcrResult> RecognizeAsync(byte[] png, AppSettings settings, Action<string> progress, Action<string> partial, CancellationToken token)
    {
        progress("正在检查小米 OCR 服务…");
        await EnsureRuntimeAsync(settings, progress, token);
        progress("正在加载模型并识别画面，首次使用可能需要稍等…");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var payload = new
        {
            model = ModelName, stream = true, keep_alive = "10m",
            messages = new[] { new { role = "user", content = "Extract all visible text in the image. Preserve reading order, line breaks and Markdown when useful. Return only the extracted text.", images = new[] { Convert.ToBase64String(png) } } },
            options = new { temperature = 0, num_predict = 4096, top_p = 0.8 }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, ServiceUrl + "/api/chat") { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"本地模型返回错误 {(int)response.StatusCode}。请检查模型是否完整。" );
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token));
            var content = new StringBuilder();
            var done = false;
            while (await reader.ReadLineAsync(timeout.Token) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("error", out var error)) throw new InvalidOperationException(error.GetString());
                if (root.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var text))
                {
                    content.Append(text.GetString());
                    partial(OcrTextFormatter.Clean(content.ToString()));
                    progress("正在提取文字，结果实时显示在右侧…");
                }
                if (root.TryGetProperty("done", out var finished) && finished.GetBoolean())
                {
                    if (root.TryGetProperty("done_reason", out var reason) && reason.GetString() == "length")
                        throw new InvalidOperationException("文本较长，已达到单次输出上限。已保留部分结果，请缩小截图区域继续识别。" );
                    done = true; break;
                }
            }
            if (!done) throw new IOException("本地模型连接中断，已保留收到的文字，请重试。" );
            return new OcrResult(OcrTextFormatter.Clean(content.ToString()), "Xiaomi OCR 0", (int)watch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new TimeoutException("识别超过 5 分钟，已停止等待。请缩小截图区域后重试。" ); }
    }

    /// <summary>Unload only this model. Terminate only the daemon process created by this service.</summary>
    public async Task StopAsync()
    {
        await StartupGate.WaitAsync().ConfigureAwait(false);
        Exception? unloadError = null;
        var terminatedOwnedProcess = false;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            try
            {
                if (await IsReadyAsync(timeout.Token).ConfigureAwait(false))
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, ServiceUrl + "/api/generate")
                    { Content = new StringContent(JsonSerializer.Serialize(new { model = ModelName, prompt = "", stream = false, keep_alive = 0 }), Encoding.UTF8, "application/json") };
                    using var response = await Client.SendAsync(request, timeout.Token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false));
                    if (!body.RootElement.TryGetProperty("done", out var done) || !done.GetBoolean() || body.RootElement.TryGetProperty("error", out _))
                        throw new InvalidOperationException("本地服务未确认模型已经卸载。");
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
            { unloadError = ex; }
            // Stop the dedicated service and its runner even if the HTTP unload timed out.
            if (Interlocked.Exchange(ref _ownedProcess, null) is { } process)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await process.WaitForExitAsync(wait.Token).ConfigureAwait(false);
                        terminatedOwnedProcess = true;
                    }
                }
                finally { process.Dispose(); }
            }
            if (unloadError is not null && !terminatedOwnedProcess)
                throw new InvalidOperationException("模型卸载未完成，请稍后重试关闭 OCR 模型。", unloadError);
        }
        finally { StartupGate.Release(); }
    }

    internal void TerminateOwnedProcess()
    {
        if (Interlocked.Exchange(ref _ownedProcess, null) is not { } process) return;
        using (process) { if (!process.HasExited) process.Kill(entireProcessTree: true); }
    }

    private async Task<bool> IsReadyAsync(CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            using var response = await Client.GetAsync(ServiceUrl + "/api/tags", timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return false;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false));
            return doc.RootElement.GetProperty("models").EnumerateArray().Any(m => m.GetProperty("name").GetString() == ModelName);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or KeyNotFoundException)
        { token.ThrowIfCancellationRequested(); return false; }
    }
}
