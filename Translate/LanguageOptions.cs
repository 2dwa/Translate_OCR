namespace Translate;

public static class LanguageOptions
{
    public static IReadOnlyList<string> Common { get; } = new[]
    {
        "简体中文", "繁體中文", "English", "日本語", "한국어", "Français", "Deutsch", "Español",
        "Português", "Italiano", "Русский", "Українська", "العربية", "עברית", "فارسی", "हिन्दी",
        "বাংলা", "தமிழ்", "ไทย", "Tiếng Việt", "Bahasa Indonesia", "Bahasa Melayu", "Türkçe",
        "Polski", "Nederlands", "Svenska", "Ελληνικά"
    };
    internal static string Validate(string? value)
    {
        var language = value?.Trim() ?? "";
        if (language.Length is < 1 or > 100 || language.Any(char.IsControl))
            throw new InvalidOperationException("请选择或输入输出语言（1–100 个字符）。");
        return language;
    }
}
