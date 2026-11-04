namespace MiOcrDesk;

internal sealed class WorkspaceOperations(WorkspaceViewModel view, OcrRuntimeService ocr, TranslationService translator)
{
    internal async Task RecognizeAsync(byte[] png, AppSettings settings, bool translateAfter, CancellationToken token)
    {
        var result = await ocr.RecognizeAsync(png, settings, text => view.StatusText = text, text => view.OcrText = text, token);
        token.ThrowIfCancellationRequested();
        view.OcrText = result.Content;
        view.OcrMeta = $"{result.Model} · {result.LatencyMs / 1000d:0.0} 秒 · 本地处理";
        view.RuntimeReady = true; view.RuntimeStatus = "本地模型已连接";
        view.StatusText = string.IsNullOrWhiteSpace(result.Content) ? "识别完成，未发现文字。请选取更清晰的区域。" : "识别完成，可以编辑、复制或翻译。";
        if (string.IsNullOrWhiteSpace(result.Content)) return;
        if (settings.AutoCopy)
        {
            try { Clipboard.SetText(result.Content); view.StatusText = "识别完成，文字已复制到剪贴板。"; }
            catch { view.StatusText = "识别完成，剪贴板暂时忙碌，请手动复制。"; }
        }
        if (translateAfter) await TranslateAsync(result.Content, settings, token);
    }

    internal async Task TranslateAsync(string source, AppSettings settings, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        view.DisplayMode = ResultDisplayMode.Compare;
        view.TranslationText = ""; view.TranslationMeta = "";
        var result = await translator.TranslateAsync(source, settings, text => view.StatusText = text, token);
        token.ThrowIfCancellationRequested();
        view.TranslationText = result.Content;
        view.TranslationMeta = $"{settings.TranslationProvider} · {result.Model} · {settings.TargetLanguage}";
        view.StatusText = "翻译完成，可复制或导出译文。";
    }
}
