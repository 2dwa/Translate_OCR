using Forms = System.Windows.Forms;

namespace MiOcrDesk;

internal sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly System.Drawing.Icon _image;
    internal TrayService(Action open, Action capture, Action settings, Action exit)
    {
        _image = (Environment.ProcessPath is { } path ? System.Drawing.Icon.ExtractAssociatedIcon(path) : null)
            ?? (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add("框选截图", null, (_, _) => capture());
        _menu.Items.Add("打开 Mi OCR Desk", null, (_, _) => open());
        _menu.Items.Add("偏好设置", null, (_, _) => settings());
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("退出", null, (_, _) => exit());
        _icon = new Forms.NotifyIcon { Icon = _image, Text = "Mi OCR Desk · 快捷键截图", ContextMenuStrip = _menu, Visible = true };
        _icon.DoubleClick += (_, _) => open();
    }
    internal void Notify(string text)
    { _icon.BalloonTipTitle = "Mi OCR Desk"; _icon.BalloonTipText = text; _icon.ShowBalloonTip(4000); }
    public void Dispose() { _icon.Visible = false; _icon.Dispose(); _menu.Dispose(); _image.Dispose(); }
}
