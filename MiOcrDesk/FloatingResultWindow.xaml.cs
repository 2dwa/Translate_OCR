using System.Windows.Interop;
using System.Windows.Input;

namespace MiOcrDesk;

public partial class FloatingResultWindow : Window
{
    public event Action? CaptureRequested, OpenRequested, CancelRequested;
    public FloatingResultWindow(WorkspaceViewModel view)
    {
        InitializeComponent(); DataContext = view;
        SourceInitialized += (_, _) => NativeMethods.TryRoundCorners(new WindowInteropHelper(this).Handle);
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            Close();
        };
    }
    internal void ShowNear(Rect? capturedRegion)
    {
        var cursor = NativeMethods.CursorPosition;
        var anchor = capturedRegion ?? new Rect(cursor.X, cursor.Y, 1, 1);
        var monitor = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)anchor.X, (int)anchor.Y)).WorkingArea;
        var handle = new WindowInteropHelper(this).EnsureHandle();
        // Move the invisible HWND to the target monitor before measuring its DPI.
        NativeMethods.SetWindowPos(handle, IntPtr.Zero, monitor.Left + 16, monitor.Top + 16, 0, 0, 0x0015);
        var dpi = NativeMethods.GetDpiForWindow(handle) / 96d;
        var placement = FloatingPlacement.Calculate(anchor, new Rect(monitor.X, monitor.Y, monitor.Width, monitor.Height), Width * dpi, Height * dpi);
        Show();
        NativeMethods.SetWindowPos(handle, IntPtr.Zero, (int)placement.X, (int)placement.Y, (int)placement.Width, (int)placement.Height, 0x0014);
        Activate();
    }
    private void Pin_Click(object sender, RoutedEventArgs e) { Topmost = !Topmost; PinButton.Content = Topmost ? "已置顶" : "置顶"; }
    private void Capture_Click(object sender, RoutedEventArgs e) => CaptureRequested?.Invoke();
    private void Open_Click(object sender, RoutedEventArgs e) => OpenRequested?.Invoke();
    private void Cancel_Click(object sender, RoutedEventArgs e) => CancelRequested?.Invoke();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

internal static class FloatingPlacement
{
    internal static Rect Calculate(Rect anchor, Rect workArea, double width, double height)
    {
        var w = Math.Min(width, Math.Max(1, workArea.Width - 24));
        var h = Math.Min(height, Math.Max(1, workArea.Height - 24));
        var x = anchor.Right + 12;
        if (x + w > workArea.Right - 12) x = anchor.Left - w - 12;
        x = Math.Clamp(x, workArea.Left + 12, workArea.Right - w - 12);
        var y = Math.Clamp(anchor.Top, workArea.Top + 12, workArea.Bottom - h - 12);
        return new Rect(x, y, w, h);
    }
}
