using System.Windows.Input;

namespace MiOcrDesk;

public partial class SettingsPage : UserControl, IDisposable
{
    private bool _initializing = true;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _modelRequest;
    private int _catalogVersion;
    private bool _loadingModels, _testing;
    private Dictionary<string, IReadOnlyList<string>> _modelCapabilities = new();
    public AppSettings Settings { get; private set; }
    public Func<AppSettings, Task>? SaveRequested { get; set; }
    public Action? ResetRequested { get; set; }
    public SettingsPage(AppSettings current)
    {
        InitializeComponent();
        Settings = current.Copy();
        Settings.EnsureProviderProfiles();
        HotkeyBox.Text = current.CaptureHotkey; AutoCopyBox.IsChecked = current.AutoCopy;
        TranslateHotkeyBox.Text = current.CaptureTranslateHotkey;
        PromptBox.Text = current.TranslationPrompt;
        LanguageBox.Text = current.TargetLanguage;
        StartAtLoginBox.IsChecked = current.StartAtLogin;
        SilentStartupBox.IsChecked = current.SilentStartup;
        BackgroundBox.IsChecked = current.RunInBackground;
        DisplayActiveProfile();
        Loaded += async (_, _) => { if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password)) await RefreshModelsAsync(); };
    }
    public void Dispose() { _lifetime.Cancel(); _modelRequest?.Cancel(); }
    private async void ProviderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || ProviderBox.SelectedValue is not string id) return;
        CaptureProviderDraft();
        _catalogVersion++; _modelRequest?.Cancel(); _loadingModels = false;
        Settings.ActivateProvider(id);
        DisplayActiveProfile();
        TestResult.Text = "已切换供应商，原供应商的密钥和设置已保留。";
        if (IsLoaded && !string.IsNullOrWhiteSpace(ApiKeyBox.Password)) await RefreshModelsAsync();
    }
    private void CaptureProviderDraft()
    {
        Settings.TranslationProvider = ProviderNameBox.Text.Trim();
        Settings.TranslationBaseUrl = BaseUrlBox.Text.Trim();
        Settings.TranslationApiKey = ApiKeyBox.Password.Trim();
        Settings.TranslationModel = ModelBox.SelectedItem as string ?? "";
        Settings.ReasoningEffort = ReasoningBox.SelectedValue as string ?? "default";
        if (int.TryParse(TimeoutBox.Text, out var timeout) && timeout is >= 15 and <= 600) Settings.TranslationTimeoutSeconds = timeout;
        Settings.StoreActiveProfile();
    }
    private void DisplayActiveProfile()
    {
        _initializing = true;
        ProviderBox.SelectedValue = Settings.TranslationProviderId;
        ProviderNameBox.Text = Settings.TranslationProvider;
        BaseUrlBox.Text = Settings.TranslationBaseUrl;
        ApiKeyBox.Password = Settings.TranslationApiKey;
        TimeoutBox.Text = Settings.TranslationTimeoutSeconds.ToString();
        _modelCapabilities.Clear();
        ModelBox.ItemsSource = string.IsNullOrWhiteSpace(Settings.TranslationModel) ? Array.Empty<string>() : new[] { Settings.TranslationModel };
        ModelBox.SelectedItem = Settings.TranslationModel;
        _initializing = false;
        ModelStatus.Text = ModelBox.SelectedItem is null ? "填写当前供应商的密钥，刷新列表后选择模型。" : "已恢复此供应商的模型选择，可刷新完整列表。";
        UpdateReasoningOptions(Settings.ReasoningEffort);
        UpdateActionStates();
    }
    private void UpdateReasoningOptions(string? preferred = null)
    {
        var model = ModelBox.SelectedItem as string ?? "";
        var selection = preferred ?? ReasoningBox.SelectedValue as string ?? "default";
        _modelCapabilities.TryGetValue(model, out var advertised);
        var options = ReasoningOptions.ForModel(Settings.TranslationProviderId, model, advertised);
        ReasoningBox.ItemsSource = options;
        ReasoningBox.SelectedValue = options.Any(option => option.Value == selection) ? selection : "default";
        ReasoningHint.Text = string.IsNullOrWhiteSpace(model) ? "选择模型后显示可用的推理设置。"
            : options.Count == 1 ? "当前模型没有已知的可调推理档位，将使用模型默认行为。"
            : Settings.TranslationProviderId == "deepseek" ? "可关闭思考或选择推理强度；优先使用服务返回的可用档位。更高强度可能增加耗时和用量。"
            : "强度越高，通常耗时和用量越高。「模型默认」使用服务默认值；具体支持情况以接口为准。";
        ReasoningBox.IsEnabled = !_loadingModels && !_testing && options.Count > 1;
    }
    private void CatalogCredentials_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _catalogVersion++;
        _modelRequest?.Cancel();
        _loadingModels = false;
        _modelCapabilities.Clear();
        ModelBox.ItemsSource = null;
        UpdateReasoningOptions();
        ModelStatus.Text = "连接信息已更改，请刷新模型列表后选择。";
        TestResult.Text = "";
        UpdateActionStates();
    }
    private void ModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;
        TestResult.Text = "";
        UpdateReasoningOptions();
        UpdateActionStates();
    }
    private async void RefreshModels_Click(object sender, RoutedEventArgs e) => await RefreshModelsAsync();
    private async Task RefreshModelsAsync()
    {
        if (_testing || _lifetime.IsCancellationRequested) return;
        _modelRequest?.Cancel();
        using var request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _modelRequest = request;
        var version = ++_catalogVersion;
        var previous = ModelBox.SelectedItem as string;
        var previousEffort = ReasoningBox.SelectedValue as string;
        var snapshot = Settings.Copy();
        snapshot.TranslationBaseUrl = BaseUrlBox.Text.Trim();
        snapshot.TranslationApiKey = ApiKeyBox.Password.Trim();
        _loadingModels = true; UpdateActionStates();
        ModelStatus.Text = "正在获取账号可见的模型…";
        try
        {
            var details = await new ModelCatalogService().ListDetailsAsync(snapshot, request.Token);
            if (version != _catalogVersion || request.IsCancellationRequested) return;
            var models = details.Select(model => model.Id).ToArray();
            _modelCapabilities = details.ToDictionary(model => model.Id, model => model.ReasoningLevels);
            _initializing = true;
            ModelBox.ItemsSource = models;
            ModelBox.SelectedItem = previous is not null && models.Contains(previous) ? previous
                : Settings.TranslationProviderId == "openai" && models.Contains(new AppSettings().TranslationModel) ? new AppSettings().TranslationModel : null;
            _initializing = false;
            UpdateReasoningOptions(previousEffort);
            ModelStatus.Text = models.Length == 0 ? "服务没有返回可选的文本模型，请检查账号权限。"
                : $"已获取 {models.Length} 个候选模型，请从列表选择；实际可用性以翻译测试为准。";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (version == _catalogVersion && !_lifetime.IsCancellationRequested)
                ModelStatus.Text = ex.Message + (ModelBox.SelectedItem is not null ? " 已保留当前选择。" : "");
        }
        finally
        {
            if (ReferenceEquals(_modelRequest, request)) _modelRequest = null;
            if (version == _catalogVersion) { _loadingModels = false; UpdateActionStates(); }
        }
    }
    private void UpdateActionStates()
    {
        ModelBox.IsEnabled = !_loadingModels && !_testing;
        RefreshModelsButton.IsEnabled = !_loadingModels && !_testing;
        TestButton.IsEnabled = !_loadingModels && !_testing && ModelBox.SelectedItem is string;
        SaveButton.IsEnabled = !_loadingModels && !_testing;
        ReasoningBox.IsEnabled = !_loadingModels && !_testing && ReasoningBox.Items.Count > 1;
        ProviderBox.IsEnabled = BaseUrlBox.IsEnabled = ApiKeyBox.IsEnabled = !_testing;
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
    internal AppSettings ReadSettings(bool requireKey = false)
    {
        HotkeyParser.ValidatePair(HotkeyBox.Text, TranslateHotkeyBox.Text);
        TranslationService.BuildEndpoint(BaseUrlBox.Text);
        var selectedModel = ModelBox.SelectedItem as string ?? "";
        if (requireKey && string.IsNullOrWhiteSpace(selectedModel)) throw new InvalidOperationException("请先刷新模型列表并选择翻译模型。" );
        if (string.IsNullOrWhiteSpace(ProviderNameBox.Text)) throw new InvalidOperationException("请填写提供商名称。" );
        if (string.IsNullOrWhiteSpace(PromptBox.Text)) throw new InvalidOperationException("请填写翻译提示词。" );
        if (!int.TryParse(TimeoutBox.Text, out var seconds) || seconds is < 15 or > 600) throw new InvalidOperationException("超时应为 15–600 秒。" );
        if (requireKey && string.IsNullOrWhiteSpace(ApiKeyBox.Password)) throw new InvalidOperationException("测试连接前请填写 API Key。" );
        var settings = Settings.Copy();
        settings.CaptureHotkey = HotkeyBox.Text.Trim(); settings.AutoCopy = AutoCopyBox.IsChecked == true;
        settings.CaptureTranslateHotkey = TranslateHotkeyBox.Text.Trim();
        settings.TranslationProvider = ProviderNameBox.Text.Trim(); settings.TranslationBaseUrl = BaseUrlBox.Text.Trim();
        settings.TranslationModel = selectedModel; settings.TranslationApiKey = ApiKeyBox.Password.Trim();
        settings.TranslationPrompt = PromptBox.Text.Trim(); settings.TranslationTimeoutSeconds = seconds;
        settings.ReasoningEffort = ReasoningBox.SelectedValue as string ?? "default";
        settings.TargetLanguage = LanguageOptions.Validate(LanguageBox.Text);
        settings.StartAtLogin = StartAtLoginBox.IsChecked == true;
        settings.SilentStartup = SilentStartupBox.IsChecked == true;
        settings.RunInBackground = BackgroundBox.IsChecked == true;
        settings.StoreActiveProfile();
        return settings;
    }
    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = ReadSettings(requireKey: true);
            _testing = true; UpdateActionStates();
            ValidationMessage.Text = "";
            var result = await new TranslationService().TranslateAsync("Hello, world.", settings, status => TestResult.Text = status, _lifetime.Token);
            TestResult.Text = "连接成功 · " + result.Content[..Math.Min(100, result.Content.Length)];
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { TestResult.Text = ex.Message; }
        finally { _testing = false; UpdateActionStates(); }
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var updated = ReadSettings();
            IsEnabled = false;
            if (SaveRequested is not null) await SaveRequested(updated);
            Settings = updated;
            ValidationMessage.Text = "设置已保存。";
        }
        catch (Exception ex) { ValidationMessage.Text = ex.Message; }
        finally { IsEnabled = true; UpdateActionStates(); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => ResetRequested?.Invoke();
}
