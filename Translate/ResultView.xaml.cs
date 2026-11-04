using System.ComponentModel;

namespace Translate;

public partial class ResultView : UserControl
{
    public static readonly DependencyProperty TextOnlyProperty = DependencyProperty.Register(nameof(TextOnly), typeof(bool), typeof(ResultView), new PropertyMetadata(false, (sender, _) => ((ResultView)sender).UpdateMode()));
    public bool TextOnly { get => (bool)GetValue(TextOnlyProperty); set => SetValue(TextOnlyProperty, value); }
    private WorkspaceViewModel? _view;
    private bool _updating;
    public event RoutedEventHandler? TranslateRequested, CopyRequested, ExportRequested, PasteRequested, RetryRequested;
    public ResultView()
    {
        InitializeComponent();
        var group = Guid.NewGuid().ToString("N");
        OriginalTab.GroupName = TranslatedTab.GroupName = CompareTab.GroupName = group;
        DataContextChanged += (_, _) => Attach();
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => { if (_view is not null) _view.PropertyChanged -= View_Changed; _view = null; };
    }
    private void Attach()
    {
        if (_view is not null) _view.PropertyChanged -= View_Changed;
        _view = DataContext as WorkspaceViewModel;
        if (_view is not null) _view.PropertyChanged += View_Changed;
        UpdateMode();
    }
    private void View_Changed(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(WorkspaceViewModel.DisplayMode)) UpdateMode(); }
    private void UpdateMode()
    {
        _updating = true;
        var mode = _view?.DisplayMode ?? ResultDisplayMode.Original;
        OriginalTab.IsChecked = mode == ResultDisplayMode.Original;
        TranslatedTab.IsChecked = mode == ResultDisplayMode.Translation;
        CompareTab.IsChecked = mode == ResultDisplayMode.Compare;
        PreviewPanel.Visibility = mode == ResultDisplayMode.Compare ? Visibility.Collapsed : Visibility.Visible;
        CompareSource.Visibility = mode == ResultDisplayMode.Compare ? Visibility.Visible : Visibility.Collapsed;
        OriginalEditor.Visibility = mode == ResultDisplayMode.Original ? Visibility.Visible : Visibility.Collapsed;
        TranslatedEditor.Visibility = mode == ResultDisplayMode.Original ? Visibility.Collapsed : Visibility.Visible;
        LeftTitle.Text = mode == ResultDisplayMode.Compare ? "原文 · 可编辑" : "原始画面";
        RightTitle.Text = mode == ResultDisplayMode.Original ? "原文 · 可编辑 / 粘贴文字" : "译文 · 可编辑";
        SourceCard.Visibility = TextOnly && mode == ResultDisplayMode.Translation ? Visibility.Collapsed : Visibility.Visible;
        TranslationCard.Visibility = TextOnly && mode == ResultDisplayMode.Original ? Visibility.Collapsed : Visibility.Visible;
        Grid.SetColumnSpan(SourceCard, TextOnly && mode == ResultDisplayMode.Original ? 3 : 1);
        Grid.SetColumn(TranslationCard, TextOnly && mode == ResultDisplayMode.Translation ? 0 : 2);
        Grid.SetColumnSpan(TranslationCard, TextOnly && mode == ResultDisplayMode.Translation ? 3 : 1);
        RetryButton.Visibility = TextOnly ? Visibility.Collapsed : Visibility.Visible;
        if (TextOnly)
        {
            PreviewPanel.Visibility = Visibility.Collapsed;
            CompareSource.Visibility = Visibility.Visible;
            OriginalEditor.Visibility = Visibility.Collapsed;
            TranslatedEditor.Visibility = Visibility.Visible;
            LeftTitle.Text = "原文 · 可编辑 / 粘贴文字";
            RightTitle.Text = "译文 · 可编辑";
        }
        _updating = false;
    }
    public void FocusSource()
    {
        if (TextOnly && _view?.DisplayMode == ResultDisplayMode.Translation) _view.DisplayMode = ResultDisplayMode.Compare;
        (TextOnly ? CompareSource : OriginalEditor).Focus();
    }
    private void Mode_Checked(object sender, RoutedEventArgs e)
    { if (!_updating && _view is not null && sender is RadioButton { Tag: string mode }) _view.DisplayMode = Enum.Parse<ResultDisplayMode>(mode); }
    private void Translate_Click(object sender, RoutedEventArgs e) => TranslateRequested?.Invoke(this, e);
    private void Copy_Click(object sender, RoutedEventArgs e) => CopyRequested?.Invoke(this, e);
    private void Export_Click(object sender, RoutedEventArgs e) => ExportRequested?.Invoke(this, e);
    private void Paste_Click(object sender, RoutedEventArgs e) => PasteRequested?.Invoke(this, e);
    private void Retry_Click(object sender, RoutedEventArgs e) => RetryRequested?.Invoke(this, e);
}
