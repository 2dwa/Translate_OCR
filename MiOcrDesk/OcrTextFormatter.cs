namespace MiOcrDesk;

internal static class OcrTextFormatter
{
    // Xiaomi OCR wraps Markdown output in these boundary tags. Preserve tags within real content.
    internal static string Clean(string text)
    {
        var result = text.Trim();
        if (result.StartsWith("<md>", StringComparison.Ordinal)) result = result[4..].TrimStart();
        if (result.EndsWith("</md>", StringComparison.Ordinal)) result = result[..^5].TrimEnd();
        return result;
    }
}
