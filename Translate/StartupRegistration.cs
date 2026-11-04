using Microsoft.Win32;

namespace Translate;

internal static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    // Reuse the startup entry when upgrading from Mi OCR Desk.
    private const string ValueName = "MiOcrDesk";
    internal static string BuildCommand(string executable, bool silent)
    {
        if (!Path.IsPathFullyQualified(executable) || executable.Contains('"')) throw new InvalidOperationException("无法确定启动程序路径。");
        var command = $"\"{executable}\"" + (silent ? " --silent" : "");
        if (command.Length > 260) throw new InvalidOperationException("安装路径过长，无法设置开机启动，请将软件移至较短路径。");
        return command;
    }
    internal static string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) as string;
    }
    internal static void Write(string? command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (command is null) key.DeleteValue(ValueName, throwOnMissingValue: false);
        else key.SetValue(ValueName, command, RegistryValueKind.String);
    }
    internal static void Apply(AppSettings settings)
    {
        Write(settings.StartAtLogin ? BuildCommand(Environment.ProcessPath ?? throw new InvalidOperationException("找不到程序路径。"), settings.SilentStartup) : null);
    }
    internal static void MigrateLegacyPath(AppSettings settings)
    {
        if (settings.StartAtLogin && Read() is { } command && command.Contains("MiOcrDesk.exe", StringComparison.OrdinalIgnoreCase))
            Apply(settings);
    }
}
