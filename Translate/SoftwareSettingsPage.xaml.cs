using System.Windows.Input;

namespace Translate;

public partial class SoftwareSettingsPage : UserControl
{
    private AppSettings _settings;
    public Func<AppSettings, Task>? SaveRequested { get; set; }
    public Action? ResetRequested { get; set; }
    public SoftwareSettingsPage(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings.Copy();
        HotkeyBox.Text = settings.CaptureHotkey;
        TranslateHotkeyBox.Text = settings.CaptureTranslateHotkey;
        SelectionHotkeyBox.Text = settings.SelectionTranslateHotkey;
        QuickHotkeyBox.Text = settings.QuickTranslateHotkey;
        LanguageBox.Text = settings.TargetLanguage;
        QuickLanguageBox.Text = settings.QuickTargetLanguage;
        SelectionLanguageBox.Text = settings.SelectionTargetLanguage;
        WorkspaceLanguageBox.Text = settings.WorkspaceTargetLanguage;
        AutoCopyBox.IsChecked = settings.AutoCopy;
        StartAtLoginBox.IsChecked = settings.StartAtLogin;
        SilentStartupBox.IsChecked = settings.SilentStartup;
        BackgroundBox.IsChecked = settings.RunInBackground;
    }
    internal AppSettings ReadSettings()
    {
        var settings = _settings.Copy();
        settings.CaptureHotkey = HotkeyBox.Text.Trim();
        settings.CaptureTranslateHotkey = TranslateHotkeyBox.Text.Trim();
        settings.SelectionTranslateHotkey = SelectionHotkeyBox.Text.Trim();
        settings.QuickTranslateHotkey = QuickHotkeyBox.Text.Trim();
        HotkeyParser.ValidateAll(settings);
        settings.TargetLanguage = LanguageOptions.Validate(LanguageBox.Text);
        settings.QuickTargetLanguage = LanguageOptions.Validate(QuickLanguageBox.Text);
        settings.SelectionTargetLanguage = LanguageOptions.Validate(SelectionLanguageBox.Text);
        settings.WorkspaceTargetLanguage = LanguageOptions.Validate(WorkspaceLanguageBox.Text);
        settings.AutoCopy = AutoCopyBox.IsChecked == true;
        settings.StartAtLogin = StartAtLoginBox.IsChecked == true;
        settings.SilentStartup = SilentStartupBox.IsChecked == true;
        settings.RunInBackground = BackgroundBox.IsChecked == true;
        return settings;
    }
    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        var modifiers = Keyboard.Modifiers;
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        var keyName = key >= Key.D0 && key <= Key.D9 ? ((int)key - (int)Key.D0).ToString() : key.ToString();
        parts.Add(keyName);
        var value = string.Join("+", parts);
        if (HotkeyParser.TryParse(value, out _, out _) && sender is TextBox box) { box.Text = value; ValidationMessage.Text = ""; }
        else ValidationMessage.Text = "请按修饰键 + 字母、数字或 F1–F24。";
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var updated = ReadSettings();
            IsEnabled = false;
            if (SaveRequested is not null) await SaveRequested(updated);
            _settings = updated;
            ValidationMessage.Text = "设置已保存。";
        }
        catch (Exception ex) { ValidationMessage.Text = ex.Message; }
        finally { IsEnabled = true; }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => ResetRequested?.Invoke();
}
