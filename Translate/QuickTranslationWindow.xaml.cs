using System.Windows.Input;
using System.Windows.Interop;

namespace Translate;

public partial class QuickTranslationWindow : Window
{
    public QuickTranslationWindow(Func<AppSettings> settings, Action openSettings)
    {
        InitializeComponent();
        Editor.SettingsProvider = settings;
        Editor.SettingsRequested += openSettings;
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; Close(); } };
        Closed += (_, _) => Editor.Dispose();
    }
    public void SetEntry(TranslationEntry entry)
    {
        Title = WindowTitle.Text = entry == TranslationEntry.Selection ? "Translate · 划词翻译" : "Translate · 输入翻译";
    }
    public void ShowNearCursor(bool activate = true)
    {
        var cursor = NativeMethods.CursorPosition;
        var area = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(cursor.X, cursor.Y)).WorkingArea;
        var handle = new WindowInteropHelper(this).EnsureHandle();
        NativeMethods.SetWindowPos(handle, IntPtr.Zero, area.Left + 12, area.Top + 12, 0, 0, 0x0015);
        var scale = NativeMethods.GetDpiForWindow(handle) / 96d;
        var bounds = FloatingPlacement.Calculate(new Rect(cursor.X, cursor.Y, 1, 1), new Rect(area.X, area.Y, area.Width, area.Height), Width * scale, Height * scale);
        NativeMethods.TryRoundCorners(handle);
        ShowActivated = activate;
        Show();
        NativeMethods.SetWindowPos(handle, IntPtr.Zero, (int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height, 0x0014);
        if (activate) { Activate(); Editor.FocusInput(); }
    }
    private void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
