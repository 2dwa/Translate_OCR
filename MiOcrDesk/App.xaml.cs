namespace MiOcrDesk;

public partial class App : Application
{
    private SingleInstance? _instance;
    private TrayService? _tray;
    private MainWindow? _window;
    private bool _exiting, _exitCleaned;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Mi OCR Desk", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        try
        {
            _instance = new SingleInstance();
            var capture = e.Args.Contains("--capture", StringComparer.OrdinalIgnoreCase);
            var silentArgument = e.Args.Contains("--silent", StringComparer.OrdinalIgnoreCase);
            if (!_instance.IsPrimary)
            {
                try { if (!silentArgument || capture) await _instance.SignalAsync(capture ? "capture" : "show"); }
                catch (Exception) { MessageBox.Show("程序正在启动，请稍后从托盘打开。", "Mi OCR Desk"); }
                Shutdown(); return;
            }
            var settings = await new SettingsStore().LoadAsync();
            _window = new MainWindow();
            MainWindow = _window;
            _tray = new TrayService(_window.OpenWorkspace, () => _ = _window.CaptureFloatingAsync(), _window.OpenPreferences, ExitApplication);
            _window.ExitRequested += ExitApplication;
            _window.Backgrounded += () => _tray.Notify("已在后台运行，可按截图快捷键。右键托盘图标可退出。");
            await _window.InitializeAsync(settings);
            if (_exiting) return;
            _ = _instance.ListenAsync(command => Dispatcher.InvokeAsync(() =>
            {
                if (command == "capture") _ = _window.CaptureFloatingAsync();
                else _window.OpenWorkspace();
            }));
            if (capture) await _window.CaptureFloatingAsync();
            else if (!silentArgument && !settings.SilentStartup) _window.OpenWorkspace();
            if (_window.HotkeyError is { } error) _tray.Notify(error);
        }
        catch (Exception ex) { MessageBox.Show("启动失败：" + ex.Message, "Mi OCR Desk"); ExitApplication(); }
    }
    private async void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        try { if (_window is not null) await _window.PrepareExitAsync(); _exitCleaned = true; }
        catch (Exception ex) { MessageBox.Show("退出清理未全部完成：" + ex.Message, "Mi OCR Desk"); }
        finally { _tray?.Dispose(); _tray = null; Shutdown(); }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        if (!_exitCleaned) { try { _window?.StopOnSystemExit(); } catch { /* Windows may already be ending the session. */ } }
        _tray?.Dispose(); _instance?.Dispose(); base.OnExit(e);
    }
}
