using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Translate;

public partial class TranslationEditor : UserControl, IDisposable
{
    private readonly ITranslationService _translator;
    private CancellationTokenSource? _request;
    private bool _disposed;
    private readonly Stopwatch _watch = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    internal WorkspaceViewModel View { get; } = new() { TargetLanguage = "英语", DisplayMode = ResultDisplayMode.Compare, ImageMeta = "Ctrl+Enter 翻译 · 支持输入或粘贴文字", StatusText = "输入文字后按 Ctrl+Enter 翻译。" };
    public Func<AppSettings> SettingsProvider { get; set; } = () => new AppSettings();
    public event Action? SettingsRequested;
    public event Action? LanguageSettingsRequested;
    public event Action? ReadCancellationRequested;
    public TranslationEditor() : this(new TranslationService()) { }
    internal TranslationEditor(ITranslationService translator)
    {
        _translator = translator;
        InitializeComponent();
        DataContext = View;
        _timer.Tick += (_, _) => View.ElapsedLabel = $"{_watch.Elapsed.TotalSeconds:0} 秒";
        PreviewKeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            { e.Handled = true; await TranslateAsync(); }
        };
    }
    public void SetInput(string text, string language)
    {
        View.DisplayMode = ResultDisplayMode.Compare;
        View.TargetLanguage = language;
        View.OcrText = text;
    }
    public void FocusInput() => Results.FocusSource();
    internal async Task TranslateAsync()
    {
        if (_disposed || !View.CanTranslate) return;
        using var request = new CancellationTokenSource();
        _request = request;
        View.IsBusy = true;
        View.DisplayMode = ResultDisplayMode.Compare;
        View.TranslationText = "";
        View.TranslationMeta = "";
        _watch.Restart(); _timer.Start(); View.ElapsedLabel = "0 秒";
        try
        {
            var settings = SettingsProvider().Copy();
            settings.TargetLanguage = LanguageOptions.Validate(View.TargetLanguage);
            var result = await _translator.TranslateAsync(View.OcrText, settings, text => View.StatusText = text, request.Token);
            request.Token.ThrowIfCancellationRequested();
            if (_disposed) return;
            View.TranslationText = result.Content;
            View.TranslationMeta = $"{settings.TranslationProvider} · {result.Model} · {settings.TargetLanguage}";
            View.StatusText = "翻译完成 · " + result.Model;
        }
        catch (OperationCanceledException) { View.StatusText = "翻译已取消。"; }
        catch (Exception ex) { View.StatusText = ex.Message; }
        finally
        {
            _timer.Stop(); _watch.Stop(); View.ElapsedLabel = $"{_watch.Elapsed.TotalSeconds:0.0} 秒";
            _request = null; View.IsBusy = false;
        }
    }
    public void SetStatus(string message) => View.StatusText = message;
    private async void Translate_Click(object sender, RoutedEventArgs e) => await TranslateAsync();
    private void Cancel_Click(object sender, RoutedEventArgs e) { _request?.Cancel(); ReadCancellationRequested?.Invoke(); }
    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();
    private void LanguageSettings_Click(object sender, RoutedEventArgs e) => LanguageSettingsRequested?.Invoke();
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!View.HasResult) return;
        try { Clipboard.SetText(View.CurrentResult); View.StatusText = "文字已复制。"; }
        catch { View.StatusText = "剪贴板正忙，请稍后重试。"; }
    }
    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (!View.HasResult) return;
        var picker = new SaveFileDialog { Title = "导出翻译文本", FileName = $"Translate-{DateTime.Now:yyyyMMdd-HHmmss}.txt", Filter = "纯文本|*.txt|Markdown|*.md" };
        if (picker.ShowDialog(Window.GetWindow(this)) != true) return;
        try { await File.WriteAllTextAsync(picker.FileName, View.CurrentResult, new UTF8Encoding(false)); View.StatusText = "文本已导出。"; }
        catch (Exception ex) { View.StatusText = "导出失败：" + ex.Message; }
    }
    public void Dispose() { _disposed = true; _timer.Stop(); _request?.Cancel(); }
}
