using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MiOcrDesk;

public partial class ScreenSelectionWindow : Window
{
    private Point _start;
    private bool _dragging;
    private readonly Rect _desktop;
    public Rect? SelectedRegion { get; private set; }
    public ScreenSelectionWindow(BitmapSource desktop, Rect bounds)
    {
        InitializeComponent();
        _desktop = bounds; DesktopImage.Source = desktop;
        Loaded += (_, _) =>
        {
            // Apply physical virtual-desktop coordinates after WPF's initial window placement.
            NativeMethods.SetWindowPos(new WindowInteropHelper(this).Handle, new IntPtr(-1), (int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height, 0x0040);
            Dispatcher.InvokeAsync(() =>
            {
                Focus();
                var cursor = NativeMethods.CursorPosition;
                var position = PointFromScreen(new Point(cursor.X, cursor.Y));
                Canvas.SetLeft(Instructions, Math.Clamp(position.X - 180, 12, Math.Max(12, ActualWidth - 440)));
                Canvas.SetTop(Instructions, Math.Clamp(position.Y + 36, 12, Math.Max(12, ActualHeight - 70)));
                UpdateShade(Rect.Empty);
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        };
        SizeChanged += (_, _) => { if (!_dragging) UpdateShade(Rect.Empty); };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; DialogResult = false; } };
    }
    private Point Clamp(Point p) => new(Math.Clamp(p.X, 0, ActualWidth), Math.Clamp(p.Y, 0, ActualHeight));
    private void Selection_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = Clamp(e.GetPosition(this)); _dragging = true; OverlayCanvas.CaptureMouse();
        Selection.Visibility = SizeBadge.Visibility = Visibility.Visible;
        Instructions.Visibility = Visibility.Collapsed;
        UpdateSelection(_start);
    }
    private void Selection_MouseMove(object sender, MouseEventArgs e) { if (_dragging) UpdateSelection(Clamp(e.GetPosition(this))); }
    private void UpdateSelection(Point end)
    {
        var rect = new Rect(_start, end);
        Canvas.SetLeft(Selection, rect.X); Canvas.SetTop(Selection, rect.Y);
        Selection.Width = rect.Width; Selection.Height = rect.Height;
        var a = PointToScreen(_start); var b = PointToScreen(end);
        SizeLabel.Text = $"{Math.Abs(b.X - a.X):0} × {Math.Abs(b.Y - a.Y):0}   松开识别";
        Canvas.SetLeft(SizeBadge, Math.Clamp(rect.X, 0, Math.Max(0, ActualWidth - 230)));
        Canvas.SetTop(SizeBadge, rect.Bottom + 38 < ActualHeight ? rect.Bottom + 8 : Math.Max(0, rect.Top - 36));
        UpdateShade(rect);
    }
    private void UpdateShade(Rect rect)
    {
        var outer = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
        Shade.Data = rect.IsEmpty ? outer : new CombinedGeometry(GeometryCombineMode.Exclude, outer, new RectangleGeometry(rect));
    }
    private void Selection_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false; OverlayCanvas.ReleaseMouseCapture();
        var a = PointToScreen(_start); var b = PointToScreen(Clamp(e.GetPosition(this)));
        var region = Rect.Intersect(new Rect(a, b), _desktop);
        if (region.Width < 4 || region.Height < 4) { DialogResult = false; return; }
        SelectedRegion = region; DialogResult = true;
    }
    private void Selection_Cancel(object sender, MouseButtonEventArgs e) => DialogResult = false;
}
