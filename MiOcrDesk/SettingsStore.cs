using System.Security.Cryptography;

namespace MiOcrDesk;

public sealed class SettingsStore
{
    private readonly string _folder;
    public SettingsStore(string? folder = null) => _folder = folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mi OCR Desk");
    private string FilePath => Path.Combine(_folder, "settings.json");

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            var json = await File.ReadAllTextAsync(FilePath);
            var raw = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            raw.TranslationApiKey = Unprotect(raw.TranslationApiKey);
            foreach (var profile in (raw.TranslationProfiles ?? new()).Values)
                profile.ApiKey = Unprotect(profile.ApiKey);
            raw.EnsureProviderProfiles();
            if (raw.TranslationPrompt == "Translate the following text into Simplified Chinese. Preserve Markdown, numbers, names, tables, and line breaks. Return only the translation.")
                raw.TranslationPrompt = new AppSettings().TranslationPrompt;
            return raw;
        }
        catch { return new AppSettings(); }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(_folder);
        var copy = settings.Copy();
        copy.EnsureProviderProfiles();
        copy.TranslationApiKey = Protect(settings.TranslationApiKey);
        foreach (var profile in copy.TranslationProfiles.Values) profile.ApiKey = Protect(profile.ApiKey);
        var json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
        var temporary = FilePath + ".tmp";
        await File.WriteAllTextAsync(temporary, json);
        File.Move(temporary, FilePath, overwrite: true);
    }

    private static string Protect(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
        return "dpapi:" + Convert.ToBase64String(bytes);
    }
    private static string Unprotect(string value)
    {
        if (!value.StartsWith("dpapi:", StringComparison.Ordinal)) return value;
        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(value[6..]), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch { return ""; }
    }
}
