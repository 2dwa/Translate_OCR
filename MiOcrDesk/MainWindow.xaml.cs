using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MiOcrDesk;

public partial class MainWindow : Window
{
    private readonly WorkspaceViewModel _view = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly OcrRuntimeService _ocr = new();
    private readonly TranslationService _translator = new();
    private readonly WorkspaceOperations _workspaceOperations;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch _watch = new();
    private AppSettings _settings = new();
    private HwndSource? _source;
    private CancellationTokenSource? _operation;
    private byte[]? _lastImage;
    private bool _selecting, _savingSettings, _loaded, _closing, _backgroundHintShown;
    private FloatingResultWindow? _floating;
    private SettingsPage? _preferences;
    private TaskCompletionSource? _operationFinished;
    public event Action? ExitRequested, Backgrounded;
    internal string? HotkeyError { get; private set; }
    private HotkeyRegistry? _hotkeys;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _view;
        _workspaceOperations = new WorkspaceOperations(_view, _ocr, _translator);
        _timer.Tick += (_, _) => _view.ElapsedLabel = $"{_watch.Elapsed.TotalSeconds:0} 秒";
        SourceInitialized += (_, _) =>
        {
            _source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            var handle = new WindowInteropHelper(this).Handle;
            _hotkeys = new HotkeyRegistry((id, modifiers, key) => NativeMethods.RegisterHotKey(handle, id, modifiers, key), id => NativeMethods.UnregisterHotKey(handle, id));
            _source?.AddHook(WindowProc);
            NativeMethods.TryRoundCorners(new WindowInteropHelper(this).Handle);
        };
        Closed += (_, _) =>
        {
            _closing = true; _operation?.Cancel(); _timer.Stop();
            _hotkeys?.Clear();
            if (_source is not null) _source.RemoveHook(WindowProc);
        };
        Closing += (_, e) =>
        {
            if (!_loaded || _closing) return;
            e.Cancel = true;
            if (_settings.RunInBackground)
            {
                Hide();
                if (!_backgroundHintShown) { _backgroundHintShown = true; Backgrounded?.Invoke(); }
            }
            else ExitRequested?.Invoke();
        };
        PreviewKeyDown += async (_, e) =>
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.FocusedElement is not TextBox)
            { e.Handled = true; await PasteImageAsync(); }
        };
    }

    internal async Task InitializeAsync(AppSettings settings)
    {
        _settings = settings;
        _view.TargetLanguage = settings.TargetLanguage;
        _view.HotkeyLabel = settings.CaptureHotkey.Replace("+", " + ");
        new WindowInteropHelper(this).EnsureHandle(); // Register even when this window has never been shown.
        var failedHotkeys = new List<string>();
        if (_hotkeys?.TryAdd(NativeMethods.CaptureHotkeyId, settings.CaptureHotkey) != true) failedHotkeys.Add("截图");
        if (_hotkeys?.TryAdd(NativeMethods.CaptureTranslateHotkeyId, settings.CaptureTranslateHotkey) != true) failedHotkeys.Add("截图并翻译");
        if (failedHotkeys.Count > 0)
            _view.StatusText = HotkeyError = string.Join("、", failedHotkeys) + "快捷键无效或已被占用，请在偏好设置中更换。";
        _loaded = true;
        await RefreshRuntimeAsync();
    }
    internal void OpenWorkspace()
    {
        if (_closing || _selecting) return;
        _floating?.Hide();
        Show(); if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }
    internal void OpenPreferences()
    {
        OpenWorkspace(); SettingsNav.IsChecked = true;
    }
    internal Task CaptureFloatingAsync() => BeginCaptureAsync(floating: true);
    internal Task CaptureAndTranslateAsync() => BeginCaptureAsync(floating: true, translateAfter: true);
    internal async Task PrepareExitAsync()
    {
        if (_closing) return;
        _closing = true;
        _preferences?.Dispose();
        _operation?.Cancel();
        _view.StatusText = "正在卸载 OCR 模型并退出…";
        _hotkeys?.Clear();
        if (_operationFinished is { } operation)
        {
            try { await operation.Task.WaitAsync(TimeSpan.FromSeconds(5)); } catch (TimeoutException) { }
        }
        await _ocr.StopAsync();
        _floating?.Close();
    }
    internal void StopOnSystemExit()
    {
        _closing = true;
        _operation?.Cancel();
        // Windows shutdown cannot wait on a continuation queued to this UI dispatcher.
        try { Task.Run(() => _ocr.StopAsync()).Wait(TimeSpan.FromSeconds(10)); }
        finally { _ocr.TerminateOwnedProcess(); }
    }
    private bool CanStart => _loaded && !_view.IsBusy && !_selecting && !_savingSettings && !_closing;
    private async void CaptureButton_Click(object sender, RoutedEventArgs e) => await BeginCaptureAsync();
    private async Task BeginCaptureAsync(bool floating = false, bool translateAfter = false)
    {
        if (!CanStart) return;
        AppSettings? translationSettings = null;
        if (translateAfter && !TryGetTranslationSettings(out translationSettings)) return;
        _selecting = true;
        byte[]? png = null;
        Rect? selectedRegion = null;
        var mainWasVisible = IsVisible;
        var floatingWasVisible = _floating?.IsVisible == true;
        try
        {
            Hide();
            _floating?.Hide();
            await Task.Delay(180); // Let the compositor remove this window before freezing the screen.
            if (_closing) return;
            var bounds = ScreenCapture.DesktopBounds;
            var desktop = ScreenCapture.Decode(ScreenCapture.CapturePng(bounds));
            var overlay = new ScreenSelectionWindow(desktop, bounds);
            if (overlay.ShowDialog() == true && overlay.SelectedRegion is { } region)
            { png = ScreenCapture.CropPng(desktop, region, bounds); selectedRegion = region; }
            else _view.StatusText = "已取消截图，随时可以再试。";
        }
        catch (Exception ex) { _view.StatusText = "截图失败：" + ex.Message; }
        finally
        {
            _selecting = false;
            if (!_closing)
            {
                if (mainWasVisible && (!floating || png is null)) OpenWorkspace();
                if (floatingWasVisible && png is null) { _floating?.Show(); _floating?.Activate(); }
            }
        }
        if (_closing) return;
        if (png is not null && floating) ShowFloating(selectedRegion);
        if (png is not null) await RecognizeAsync(png, "屏幕截图", translationSettings);
    }
    private void ShowFloating(Rect? region)
    {
        if (_floating is null)
        {
            var window = new FloatingResultWindow(_view);
            window.Results.TranslateRequested += TranslateButton_Click;
            window.Results.CopyRequested += CopyResult_Click;
            window.Results.ExportRequested += ExportResult_Click;
            window.Results.PasteRequested += PasteButton_Click;
            window.Results.RetryRequested += RetryButton_Click;
            window.CaptureRequested += () => _ = CaptureFloatingAsync();
            window.OpenRequested += OpenWorkspace;
            window.CancelRequested += () => _operation?.Cancel();
            window.Closed += (_, _) => { _operation?.Cancel(); _floating = null; };
            _floating = window;
        }
        _floating.ShowNear(region);
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStart) return;
        var picker = new OpenFileDialog { Title = "选择要识别的图片", Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff", CheckFileExists = true };
        if (picker.ShowDialog(this) == true) await ImportFileAsync(picker.FileName);
    }
    private async Task ImportFileAsync(string path)
    {
        if (!CanStart) return;
        try
        {
            if (new FileInfo(path).Length > 30 * 1024 * 1024) throw new InvalidOperationException("图片不能超过 30 MB，请缩小后重试。" );
            var image = ScreenCapture.Decode(await File.ReadAllBytesAsync(path));
            await RecognizeAsync(ScreenCapture.EncodePng(image), Path.GetFileName(path));
        }
        catch (Exception ex) { _view.StatusText = "导入失败：" + ex.Message; }
    }
    private async void PasteButton_Click(object sender, RoutedEventArgs e) => await PasteImageAsync();
    private async Task PasteImageAsync()
    {
        if (!CanStart) return;
        try
        {
            if (Clipboard.ContainsImage() && Clipboard.GetImage() is { } image) await RecognizeAsync(ScreenCapture.EncodePng(image), "剪贴板图片");
            else _view.StatusText = "剪贴板中没有图片，请先复制一张图片。";
        }
        catch (Exception ex) { _view.StatusText = "读取剪贴板失败：" + ex.Message; }
    }
    private async void Window_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (CanStart && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) await ImportFileAsync(files[0]);
    }
    private void Window_DragOver(object sender, DragEventArgs e)
    { e.Effects = CanStart && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; }
    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    { if (CanStart && _lastImage is not null) await RecognizeAsync(_lastImage, "重新识别"); }

    private async Task RecognizeAsync(byte[] png, string name, AppSettings? translationSettings = null)
    {
        if (!CanStart) return;
        try { _view.Preview = ScreenCapture.Decode(png); }
        catch (Exception ex) { _view.StatusText = "无法读取图片：" + ex.Message; return; }
        _lastImage = png;
        _view.ImageMeta = $"{name} · {_view.Preview.PixelWidth} × {_view.Preview.PixelHeight}";
        _view.OcrText = _view.TranslationText = "";
        _view.OcrMeta = _view.TranslationMeta = "";
        if (IsVisible) WorkspaceNav.IsChecked = true;
        _view.DisplayMode = ResultDisplayMode.Original;
        var snapshot = translationSettings ?? _settings.Copy();
        await RunOperationAsync(token => _workspaceOperations.RecognizeAsync(png, snapshot, translationSettings is not null, token));
    }

    private async void TranslateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStart || !_view.CanTranslate) return;
        if (!TryGetTranslationSettings(out var snapshot)) return;
        var sourceText = _view.OcrText;
        await RunOperationAsync(token => _workspaceOperations.TranslateAsync(sourceText, snapshot!, token));
    }
    private bool TryGetTranslationSettings(out AppSettings? snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(_settings.TranslationApiKey) || string.IsNullOrWhiteSpace(_settings.TranslationModel))
        { _view.StatusText = "先在偏好设置中填写 API Key，并从列表选择翻译模型。"; OpenPreferences(); return false; }
        try
        {
            snapshot = _settings.Copy();
            snapshot.TargetLanguage = LanguageOptions.Validate(_view.TargetLanguage);
            TranslationService.BuildEndpoint(snapshot.TranslationBaseUrl);
            return true;
        }
        catch (Exception ex) { _view.StatusText = ex.Message; OpenPreferences(); return false; }
    }
    private async Task RunOperationAsync(Func<CancellationToken, Task> action)
    {
        if (_view.IsBusy) return;
        using var cancellation = new CancellationTokenSource();
        _operation = cancellation;
        var completion = _operationFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _view.IsBusy = true; _view.ElapsedLabel = "0 秒"; _watch.Restart(); _timer.Start();
        try { await action(cancellation.Token); }
        catch (OperationCanceledException) { _view.StatusText = "操作已取消，已收到的文字会保留。"; }
        catch (Exception ex) { _view.StatusText = ex is HttpRequestException ? "无法连接服务，请检查本地引擎或翻译接口地址后重试。" : ex.Message; }
        finally
        {
            _timer.Stop(); _watch.Stop(); _view.ElapsedLabel = $"{_watch.Elapsed.TotalSeconds:0.0} 秒";
            _operation = null; _view.IsBusy = false;
            _operationFinished = null; completion.TrySetResult();
        }
    }
    private void SetStatus(string text) => _view.StatusText = text;
    private void CancelOperation_Click(object sender, RoutedEventArgs e) { _operation?.Cancel(); _view.StatusText = "正在取消…"; }

    private void CopyResult_Click(object sender, RoutedEventArgs e)
    {
        if (!_view.HasResult) return;
        try { Clipboard.SetText(_view.CurrentResult); _view.StatusText = "文字已复制到剪贴板。"; }
        catch { _view.StatusText = "剪贴板正在被其他程序使用，请稍后重试。"; }
    }
    private async void ExportResult_Click(object sender, RoutedEventArgs e)
    {
        if (!_view.HasResult) return;
        var picker = new SaveFileDialog { Title = "导出文本", FileName = $"MiOCR-{DateTime.Now:yyyyMMdd-HHmmss}.txt", Filter = "纯文本|*.txt|Markdown|*.md" };
        if (picker.ShowDialog(_floating?.IsVisible == true ? _floating : this) != true) return;
        try { await File.WriteAllTextAsync(picker.FileName, _view.CurrentResult, new UTF8Encoding(false)); _view.StatusText = "文本已导出。"; }
        catch (Exception ex) { _view.StatusText = "导出失败：" + ex.Message; }
    }

    private void SettingsNav_Checked(object sender, RoutedEventArgs e)
    {
        if (SettingsHost is null) return;
        if (_preferences is null) ResetPreferences();
        ShowPage(SettingsHost);
    }
    private void ResetPreferences()
    {
        _preferences?.Dispose();
        _preferences = new SettingsPage(_settings) { SaveRequested = SavePreferencesAsync, ResetRequested = ResetPreferences };
        SettingsHost.Content = _preferences;
    }
    private async Task SavePreferencesAsync(AppSettings updated)
    {
        if (!CanStart) throw new InvalidOperationException("请等待当前任务结束后再保存设置。");
        _savingSettings = true;
        var previousHotkeys = _hotkeys?.Snapshot();
        var hotkeysChanged = false;
        string? previousStartup = null;
        var startupChanged = false;
        try
        {
            if (_hotkeys is null) throw new InvalidOperationException("快捷键服务尚未就绪，请稍后再试。");
            _hotkeys.Apply(updated.CaptureHotkey, updated.CaptureTranslateHotkey); hotkeysChanged = true;
            previousStartup = StartupRegistration.Read();
            StartupRegistration.Apply(updated); startupChanged = true;
            // Preserve runtime changes made on the model page after this draft was opened.
            updated.RuntimeRoot = _settings.RuntimeRoot;
            await _settingsStore.SaveAsync(updated);
            _settings = updated;
            _view.HotkeyLabel = _settings.CaptureHotkey.Replace("+", " + ");
            _view.TargetLanguage = _settings.TargetLanguage;
            HotkeyError = null;
            _view.StatusText = "偏好设置已保存。";
        }
        catch
        {
            if (hotkeysChanged && previousHotkeys is not null && _hotkeys?.Restore(previousHotkeys) != true)
                _view.StatusText = HotkeyError = "部分原快捷键已被其他程序占用，请在偏好设置中重新设置。";
            if (startupChanged) StartupRegistration.Write(previousStartup);
            throw;
        }
        finally { _savingSettings = false; }
    }
    private async void ChooseRuntime_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStart) return;
        var picker = new OpenFolderDialog { Title = "选择包含 runtime 与 models 的离线模型目录" };
        if (picker.ShowDialog(this) != true) return;
        if (OcrRuntimeService.ValidateBundle(picker.FolderName) is { } error) { _view.StatusText = error; return; }
        try
        {
            var updated = _settings.Copy(); updated.RuntimeRoot = picker.FolderName;
            await _settingsStore.SaveAsync(updated); _settings = updated;
            await RefreshRuntimeAsync(); _view.StatusText = "已关联本地模型目录。已运行的服务将继续复用，新目录在下次引擎启动时生效。";
        }
        catch (Exception ex) { _view.StatusText = "保存模型目录失败：" + ex.Message; }
    }
    private async void InitializeRuntime_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStart) return;
        await RunOperationAsync(async token => { await _ocr.EnsureRuntimeAsync(_settings, SetStatus, token); await RefreshRuntimeAsync(token); _view.StatusText = "本地模型已连接，现在可以开始截图识别。"; });
    }
    private async void StopRuntime_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStart) return;
        await RunOperationAsync(async _ =>
        {
            SetStatus("正在卸载 OCR 模型…");
            await _ocr.StopAsync();
            await RefreshRuntimeAsync();
            _view.RuntimeStatus = "OCR 模型已关闭";
            _view.RuntimeReady = false;
            _view.RuntimeDetail = "模型已卸载，下次截图时会自动启动并重新加载。";
            SetStatus("OCR 模型已关闭，下次截图时会自动加载。");
        });
    }
    private async void RefreshRuntime_Click(object sender, RoutedEventArgs e)
    { if (CanStart) await RunOperationAsync(async token => { SetStatus("正在检测本地环境…"); await RefreshRuntimeAsync(token); SetStatus(_view.RuntimeDetail); }); }
    private async Task RefreshRuntimeAsync(CancellationToken token = default)
    {
        try
        {
            var state = await _ocr.InspectAsync(_settings, token);
            _view.RuntimePath = state.Root; _view.RuntimeReady = state.Ready;
            _view.RuntimeStatus = state.Ready ? "本地模型已连接" : state.Installed ? "模型已安装 · 待启动" : "等待安装本地模型";
            _view.RuntimeDetail = state.Detail;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { _view.RuntimeReady = false; _view.RuntimeStatus = "环境检测失败"; _view.RuntimeDetail = ex.Message; }
    }
    private void ShowPage(FrameworkElement page)
    {
        if (WorkspacePage is null || ModelPage is null || SettingsHost is null) return;
        foreach (var candidate in new FrameworkElement[] { WorkspacePage, ModelPage, SettingsHost })
            candidate.Visibility = candidate == page ? Visibility.Visible : Visibility.Collapsed;
    }
    private void WorkspaceNav_Checked(object sender, RoutedEventArgs e) { if (WorkspacePage is not null) ShowPage(WorkspacePage); }
    private void ModelNav_Checked(object sender, RoutedEventArgs e) { if (ModelPage is not null) ShowPage(ModelPage); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == NativeMethods.CaptureHotkeyId)
        { handled = true; _ = Dispatcher.InvokeAsync(CaptureFloatingAsync); }
        else if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == NativeMethods.CaptureTranslateHotkeyId)
        { handled = true; _ = Dispatcher.InvokeAsync(CaptureAndTranslateAsync); }
        return IntPtr.Zero;
    }
}
