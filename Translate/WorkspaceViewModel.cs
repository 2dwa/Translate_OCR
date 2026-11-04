using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Translate;

public sealed class WorkspaceViewModel : INotifyPropertyChanged
{
    private string _ocrText = "", _translationText = "", _statusText = "准备就绪，框选屏幕或导入一张图片。";
    private string _ocrMeta = "", _translationMeta = "", _elapsedLabel = "", _imageMeta = "支持 PNG · JPG · BMP · TIFF";
    private string _hotkeyLabel = "Ctrl + Alt + O", _runtimeStatus = "正在检测本地环境", _runtimeDetail = "", _runtimePath = "";
    private bool _isBusy, _runtimeReady;
    private ResultDisplayMode _displayMode;
    private string _targetLanguage = "简体中文";
    private BitmapSource? _preview;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string OcrText { get => _ocrText; set { if (_ocrText == value) return; Set(ref _ocrText, value); TranslationText = ""; TranslationMeta = ""; RefreshResult(); } }
    public string TranslationText { get => _translationText; set { Set(ref _translationText, value); RefreshResult(); } }
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public string OcrMeta { get => _ocrMeta; set { Set(ref _ocrMeta, value); Changed(nameof(ResultMeta)); } }
    public string TranslationMeta { get => _translationMeta; set { Set(ref _translationMeta, value); Changed(nameof(ResultMeta)); } }
    public string ElapsedLabel { get => _elapsedLabel; set => Set(ref _elapsedLabel, value); }
    public string ImageMeta { get => _imageMeta; set => Set(ref _imageMeta, value); }
    public string HotkeyLabel { get => _hotkeyLabel; set => Set(ref _hotkeyLabel, value); }
    public string RuntimeStatus { get => _runtimeStatus; set => Set(ref _runtimeStatus, value); }
    public string RuntimeDetail { get => _runtimeDetail; set => Set(ref _runtimeDetail, value); }
    public string RuntimePath { get => _runtimePath; set => Set(ref _runtimePath, value); }
    public bool RuntimeReady { get => _runtimeReady; set { Set(ref _runtimeReady, value); Changed(nameof(RuntimeBrush)); } }
    public Brush RuntimeBrush => RuntimeReady ? new SolidColorBrush(Color.FromRgb(52, 199, 89)) : new SolidColorBrush(Color.FromRgb(255, 159, 10));
    public bool IsBusy { get => _isBusy; set { Set(ref _isBusy, value); Changed(nameof(CanInteract)); Changed(nameof(BusyVisibility)); Changed(nameof(CanRetry)); Changed(nameof(StatusGlyph)); RefreshResult(); } }
    public ResultDisplayMode DisplayMode { get => _displayMode; set { Set(ref _displayMode, value); Changed(nameof(IsTranslation)); RefreshResult(); } }
    public bool IsTranslation { get => DisplayMode != ResultDisplayMode.Original; set => DisplayMode = value ? ResultDisplayMode.Translation : ResultDisplayMode.Original; }
    public string TargetLanguage { get => _targetLanguage; set { if (_targetLanguage == value) return; Set(ref _targetLanguage, value); TranslationText = ""; TranslationMeta = ""; } }
    public BitmapSource? Preview { get => _preview; set { Set(ref _preview, value); Changed(nameof(EmptyPreviewVisibility)); Changed(nameof(CanRetry)); } }
    public bool CanInteract => !IsBusy;
    public bool CanRetry => !IsBusy && Preview is not null;
    public bool CanTranslate => !IsBusy && !string.IsNullOrWhiteSpace(OcrText);
    public bool HasResult => !string.IsNullOrWhiteSpace(CurrentResult);
    public string CurrentResult => DisplayMode == ResultDisplayMode.Compare
        ? string.IsNullOrWhiteSpace(OcrText) && string.IsNullOrWhiteSpace(TranslationText) ? "" : $"原文\n{OcrText}\n\n译文（{TargetLanguage}）\n{TranslationText}"
        : IsTranslation ? TranslationText : OcrText;
    public string ResultLabel => IsTranslation ? "译文 · 可直接编辑" : "原文 · 可直接编辑";
    public string ResultMeta => IsTranslation ? TranslationMeta : OcrMeta;
    public string CharacterCount => $"{CurrentResult.Length:N0} 字符";
    public string StatusGlyph => IsBusy ? "\uE895" : "\uE73E";
    public string EmptyResultTitle => IsTranslation ? "让文字跨越语言" : "文字将在这里呈现";
    public string EmptyResultHint => IsTranslation ? "在 API 设置中连接模型，点击「翻译文本」" : "识别后可编辑、复制，或继续翻译";
    public Visibility EmptyResultVisibility => HasResult ? Visibility.Collapsed : Visibility.Visible;
    public Visibility EmptyPreviewVisibility => Preview is null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BusyVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;
    private void RefreshResult()
    {
        foreach (var name in new[] { nameof(CurrentResult), nameof(ResultLabel), nameof(ResultMeta), nameof(CharacterCount), nameof(HasResult), nameof(CanTranslate), nameof(EmptyResultTitle), nameof(EmptyResultHint), nameof(EmptyResultVisibility) }) Changed(name);
    }
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; Changed(name); }
    private void Changed(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum ResultDisplayMode { Original, Translation, Compare }
