using System.Security.Cryptography;

namespace Translate;

public sealed class SettingsStore
{
    private readonly string _folder;
    private readonly string? _legacyFolder;
    public SettingsStore(string? folder = null, string? legacyFolder = null)
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _folder = folder ?? Path.Combine(applicationData, "Translate");
        _legacyFolder = legacyFolder ?? (folder is null ? Path.Combine(applicationData, "Mi OCR Desk") : null);
    }
    private string FilePath => Path.Combine(_folder, "settings.json");

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            var source = File.Exists(FilePath) ? FilePath : _legacyFolder is null ? null : Path.Combine(_legacyFolder, "settings.json");
            if (source is null || !File.Exists(source)) return new AppSettings();
            var json = await File.ReadAllTextAsync(source);
            var raw = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(AppSettings.SelectionTargetLanguage), out _) || string.IsNullOrWhiteSpace(raw.SelectionTargetLanguage))
                raw.SelectionTargetLanguage = raw.TargetLanguage;
            if (!document.RootElement.TryGetProperty(nameof(AppSettings.WorkspaceTargetLanguage), out _) || string.IsNullOrWhiteSpace(raw.WorkspaceTargetLanguage))
                raw.WorkspaceTargetLanguage = raw.QuickTargetLanguage;
            raw.TranslationApiKey = Unprotect(raw.TranslationApiKey);
            foreach (var profile in (raw.TranslationProfiles ?? new()).Values)
                profile.ApiKey = Unprotect(profile.ApiKey);
            raw.EnsureProviderProfiles();
            if (raw.TranslationPrompt == "Translate the following text into Simplified Chinese. Preserve Markdown, numbers, names, tables, and line breaks. Return only the translation.")
                raw.TranslationPrompt = new AppSettings().TranslationPrompt;
            if (source != FilePath)
            {
                // Preserve the loaded configuration even if migration cannot be saved yet.
                try { await SaveAsync(raw); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
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
