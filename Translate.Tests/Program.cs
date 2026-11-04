using System.Net;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Translate;

internal static class Program
{
    private static int _passed;
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--runtime-child")) { Thread.Sleep(30_000); return 0; }
        try
        {
            var app = new App(); app.InitializeComponent(); app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Test("XAML resources and window construction", () =>
            {
                var main = new MainWindow();
                var settings = new SettingsPage(new AppSettings());
                var modelPicker = (System.Windows.Controls.ComboBox)settings.FindName("ModelBox");
                Check(!modelPicker.IsEditable && (string?)modelPicker.SelectedItem == "gpt-4.1-mini");
                ((System.Windows.Controls.TextBox)settings.FindName("BaseUrlBox")).Text = "https://example.com/v1";
                Check(modelPicker.Items.Count == 0 && modelPicker.SelectedItem is null);
                var overlay = new ScreenSelectionWindow(CreateImage(), new Rect(-100, 0, 600, 180));
                var floating = new FloatingResultWindow(new WorkspaceViewModel());
                Check(!main.IsVisible && !floating.IsVisible && !floating.ShowInTaskbar);
                Check(main.FindName("SettingsHost") is System.Windows.Controls.ContentControl);
                Check((object)settings is System.Windows.Controls.UserControl);
                main.Close(); settings.Dispose(); overlay.Close(); floating.Close();
            });
            Test("Result views synchronize comparison mode and preserve Unicode", () =>
            {
                var vm = new WorkspaceViewModel { OcrText = "Hello 你好 日本語 العربية हिन्दी 🙂", TargetLanguage = "Українська", TranslationText = "Привіт" };
                var first = new ResultView { DataContext = vm };
                var second = new ResultView { DataContext = vm };
                ((System.Windows.Controls.RadioButton)first.FindName("CompareTab")).IsChecked = true;
                Check(vm.DisplayMode == ResultDisplayMode.Compare);
                Check(((System.Windows.Controls.RadioButton)second.FindName("CompareTab")).IsChecked == true);
                Check(((System.Windows.Controls.TextBox)first.FindName("CompareSource")).Visibility == Visibility.Visible);
                Check(vm.CurrentResult.Contains(vm.OcrText) && vm.CurrentResult.Contains("Привіт") && vm.CurrentResult.Contains("Українська"));
                vm.OcrText += "!";
                Check(vm.TranslationText == "");
                vm.TranslationText = "new translation"; vm.TargetLanguage = "ไทย";
                Check(vm.TranslationText == "");
                vm.DisplayMode = ResultDisplayMode.Original;
                Check(((System.Windows.Controls.TextBox)second.FindName("OriginalEditor")).Visibility == Visibility.Visible);
                Check(vm.CurrentResult == vm.OcrText);
            });
            Test("Floating placement stays inside multi-monitor work areas at scaled sizes", () =>
            {
                foreach (var area in new[] { new Rect(-1920, 0, 1920, 1040), new Rect(0, -1080, 1920, 1080), new Rect(0, 0, 1366, 728) })
                foreach (var scale in new[] { 1d, 1.5, 2d })
                {
                    var placed = FloatingPlacement.Calculate(new Rect(area.Right - 30, area.Bottom - 30, 20, 20), area, 720 * scale, 600 * scale);
                    Check(area.Contains(placed) && placed.Width > 0 && placed.Height > 0);
                }
                var next = FloatingPlacement.Calculate(new Rect(40, 100, 200, 150), new Rect(0, 0, 1920, 1040), 720, 600);
                Check(next.X == 252 && next.Y == 100);
            });
            Test("Startup commands quote paths and keep silent startup optional", () =>
            {
                Check(StartupRegistration.BuildCommand(@"D:\My Apps\Translate.exe", true) == "\"D:\\My Apps\\Translate.exe\" --silent");
                Check(StartupRegistration.BuildCommand(@"D:\Apps\Translate.exe", false) == "\"D:\\Apps\\Translate.exe\"");
                Throws<InvalidOperationException>(() => StartupRegistration.BuildCommand("relative.exe", false));
                Throws<InvalidOperationException>(() => StartupRegistration.BuildCommand("D:\\" + new string('a', 270) + ".exe", true));
            });
            Test("Embedded preferences accept arbitrary target languages and startup options", () =>
            {
                var page = new SoftwareSettingsPage(new AppSettings { TargetLanguage = "မြန်မာဘာသာ", StartAtLogin = true, SilentStartup = true, RunInBackground = false });
                var language = (System.Windows.Controls.ComboBox)page.FindName("LanguageBox");
                Check(language.IsEditable && language.Text == "မြန်မာဘာသာ");
                var saved = page.ReadSettings();
                Check(saved.TargetLanguage == "မြန်မာဘာသာ" && saved.StartAtLogin && saved.SilentStartup && !saved.RunInBackground);
                language.Text = "Ελληνικά";
                Check(page.ReadSettings().TargetLanguage == "Ελληνικά");
                language.Text = " "; Throws<InvalidOperationException>(() => page.ReadSettings());
            });
            Test("Repeated launch signals the existing instance without starting another", () =>
            {
                var scope = "tests-" + Guid.NewGuid().ToString("N");
                using var first = new SingleInstance(scope);
                using var second = new SingleInstance(scope);
                Check(first.IsPrimary && !second.IsPrimary);
                var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = first.ListenAsync(command => received.TrySetResult(command));
                second.SignalAsync("show").GetAwaiter().GetResult();
                Check(received.Task.WaitAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult() == "show");
            });
            Test("Hotkey parser", () =>
            {
                Check(HotkeyParser.TryParse("Ctrl+Alt+O", out var modifiers, out var key) && modifiers == 3 && key == 'O');
                Check(HotkeyParser.TryParse("ctrl+f12", out _, out key) && key == 0x7b);
                foreach (var invalid in new[] { "O", "Ctrl", "Ctrl+A+B", "Ctrl+中", "Alt+F25" }) Check(!HotkeyParser.TryParse(invalid, out _, out _));
            });
            Test("Capture and translate shortcuts validate independently and reject equivalent combinations", () =>
            {
                var page = new SoftwareSettingsPage(new AppSettings());
                var capture = (System.Windows.Controls.TextBox)page.FindName("HotkeyBox");
                var translate = (System.Windows.Controls.TextBox)page.FindName("TranslateHotkeyBox");
                Check(capture.Text == "Ctrl+Alt+O" && translate.Text == "Ctrl+Alt+T");
                translate.Text = "Ctrl+Shift+F9";
                Check(page.ReadSettings().CaptureTranslateHotkey == "Ctrl+Shift+F9" && page.ReadSettings().CaptureHotkey == "Ctrl+Alt+O");
                translate.Text = "alt+CONTROL+o";
                Throws<InvalidOperationException>(() => page.ReadSettings());
                translate.Text = "T";
                Throws<InvalidOperationException>(() => page.ReadSettings());
                Check(JsonSerializer.Deserialize<AppSettings>("{\"CaptureHotkey\":\"Ctrl+Shift+F8\"}")!.CaptureTranslateHotkey == "Ctrl+Alt+T");
            });
            Test("Two hotkeys support swapping, conflict rollback and complete unregister", () =>
            {
                var registered = new Dictionary<int, (uint Modifiers, uint Key)>();
                var registry = new HotkeyRegistry((id, modifiers, key) =>
                {
                    Check((modifiers & 0x4000) != 0);
                    if (key == 'X' || registered.Values.Contains((modifiers, key))) return false;
                    registered.Add(id, (modifiers, key)); return true;
                }, id => registered.Remove(id));
                registry.Apply("Ctrl+Alt+O", "Ctrl+Alt+T");
                var capture = NativeMethods.CaptureHotkeyId; var translate = NativeMethods.CaptureTranslateHotkeyId;
                Check(registered.Count == 2 && registered[capture].Key == 'O' && registered[translate].Key == 'T');
                registry.Apply("Ctrl+Alt+T", "Ctrl+Alt+O");
                Check(registered[capture].Key == 'T' && registered[translate].Key == 'O');
                Throws<InvalidOperationException>(() => registry.Apply("Ctrl+Alt+R", "Ctrl+Alt+X"));
                Check(registered.Count == 2 && registered[capture].Key == 'T' && registered[translate].Key == 'O');
                Throws<InvalidOperationException>(() => registry.Apply("Ctrl+Alt+T", "Alt+Ctrl+T"));
                Check(registered.Count == 2);
                var previous = registry.Snapshot();
                registry.Apply("Ctrl+Shift+A", "Ctrl+Shift+B");
                Check(registry.Restore(previous) && registered[capture].Key == 'T');
                registry.Clear(); Check(registered.Count == 0);
                Check(!registry.TryAdd(capture, "Ctrl+Alt+X") && registry.TryAdd(translate, "Ctrl+Alt+T"));
                registry.Clear(); Check(registered.Count == 0);
            });
            Test("Four shortcuts reject cross-feature conflicts and restore all registrations", () =>
            {
                var settings = new AppSettings();
                HotkeyParser.ValidateAll(settings);
                var registered = new Dictionary<int, uint>();
                var registry = new HotkeyRegistry((id, modifiers, key) =>
                {
                    if (key == 'X') return false;
                    registered[id] = key;
                    return true;
                }, id => registered.Remove(id));
                registry.Apply(settings);
                Check(registered.Count == 4);
                settings.QuickTranslateHotkey = "Alt+Control+Y";
                Throws<InvalidOperationException>(() => registry.Apply(settings));
                Check(registered.Count == 4);
                settings.QuickTranslateHotkey = "Ctrl+Alt+X";
                Throws<InvalidOperationException>(() => registry.Apply(settings));
                Check(registered.Count == 4 && registered[NativeMethods.QuickTranslateHotkeyId] == 'E');
            });
            Test("Translation editor shares prompt/provider settings and overrides only output language", () =>
            {
                var settings = new AppSettings { TranslationPrompt = "Name a variable in {targetLanguage}.", TranslationApiKey = "synthetic", TargetLanguage = "简体中文", ReasoningEffort = "high" };
                AppSettings? sent = null;
                using var editor = new TranslationEditor(new ImmediateTranslator((text, snapshot) =>
                {
                    Check(text == "用户名称");
                    sent = snapshot;
                    return new TranslationResult("userName", snapshot.TranslationModel);
                })) { SettingsProvider = () => settings };
                editor.SetInput("用户名称", "英语");
                editor.TranslateAsync().GetAwaiter().GetResult();
                Check(sent is not null && sent.TargetLanguage == "英语" && sent.TranslationPrompt == settings.TranslationPrompt && sent.ReasoningEffort == "high");
                Check(settings.TargetLanguage == "简体中文" && editor.View.TranslationText == "userName");
                editor.View.OcrText = "另一段文字";
                Check(editor.View.TranslationText == "");
                var popup = new QuickTranslationWindow(() => settings, () => { });
                Check(!popup.IsVisible && !popup.ShowInTaskbar);
                popup.Close();
                using var api = new SettingsPage(settings);
                var software = new SoftwareSettingsPage(settings);
                Check(api.FindName("HotkeyBox") is null && software.FindName("ApiKeyBox") is null);
                Check(((System.Windows.Controls.TextBox)api.FindName("PromptBox")).Text == settings.TranslationPrompt);
            });
            Test("Translation panels share the OCR layout and preserve mode-specific exports", () =>
            {
                var view = new WorkspaceViewModel { OcrText = "原文", TranslationText = "translation", DisplayMode = ResultDisplayMode.Compare };
                var result = new ResultView { TextOnly = true, DataContext = view };
                result.Measure(new Size(640, 420));
                result.Arrange(new Rect(0, 0, 640, 420));
                result.UpdateLayout();
                var left = (FrameworkElement)result.FindName("SourceCard");
                var right = (FrameworkElement)result.FindName("TranslationCard");
                Check(left.Visibility == Visibility.Visible && right.Visibility == Visibility.Visible);
                Check(Math.Abs(left.ActualWidth - right.ActualWidth) < 1);
                Check(right.TranslatePoint(new Point(), result).X > left.TranslatePoint(new Point(), result).X + left.ActualWidth);
                Check(((FrameworkElement)result.FindName("PreviewPanel")).Visibility == Visibility.Collapsed);
                Check(view.CurrentResult.Contains("原文") && view.CurrentResult.Contains("translation"));
                view.DisplayMode = ResultDisplayMode.Translation;
                Check(left.Visibility == Visibility.Collapsed && System.Windows.Controls.Grid.GetColumnSpan(right) == 3 && view.CurrentResult == "translation");
                view.DisplayMode = ResultDisplayMode.Original;
                Check(right.Visibility == Visibility.Collapsed && System.Windows.Controls.Grid.GetColumnSpan(left) == 3 && view.CurrentResult == "原文");
                view.DisplayMode = ResultDisplayMode.Compare;
                Check(System.Windows.Controls.Grid.GetColumnSpan(left) == 1 && System.Windows.Controls.Grid.GetColumnSpan(right) == 1);
                var popup = new QuickTranslationWindow(() => new AppSettings(), () => { });
                Check(!popup.ShowActivated && !popup.IsVisible && popup.Width == 720);
                popup.Close();
            });
            Test("Each translation entry reads its own language and keeps the shared prompt", () =>
            {
                var settings = new AppSettings { TargetLanguage = "日本語", SelectionTargetLanguage = "Français", WorkspaceTargetLanguage = "Deutsch", QuickTargetLanguage = "English", TranslationPrompt = "Use {targetLanguage} for identifiers." };
                var page = new SoftwareSettingsPage(settings);
                var saved = page.ReadSettings();
                var expected = new[] { "日本語", "Français", "Deutsch", "English" };
                var index = 0;
                foreach (var entry in Enum.GetValues<TranslationEntry>())
                {
                    var snapshot = saved.ForTranslation(entry);
                    Check(snapshot.TargetLanguage == expected[index++] && snapshot.TranslationPrompt == settings.TranslationPrompt);
                    using var editor = new TranslationEditor(new ImmediateTranslator((source, sent) =>
                    {
                        Check(sent.TargetLanguage == snapshot.TargetLanguage);
                        return new TranslationResult(sent.TargetLanguage, "fake");
                    })) { SettingsProvider = () => saved.ForTranslation(entry) };
                    editor.SetInput("文字", saved.GetTargetLanguage(entry));
                    editor.TranslateAsync().GetAwaiter().GetResult();
                    Check(editor.View.TranslationText == snapshot.TargetLanguage);
                }
                saved.SelectionTargetLanguage = "Italiano";
                Check(saved.TargetLanguage == "日本語" && saved.WorkspaceTargetLanguage == "Deutsch" && saved.QuickTargetLanguage == "English");
                ((System.Windows.Controls.ComboBox)page.FindName("SelectionLanguageBox")).Text = "العربية";
                Check(page.ReadSettings().SelectionTargetLanguage == "العربية" && page.ReadSettings().TargetLanguage == "日本語");
                ((System.Windows.Controls.ComboBox)page.FindName("WorkspaceLanguageBox")).Text = " ";
                Throws<InvalidOperationException>(() => page.ReadSettings());
            });
            Test("Selection reading uses an STA worker and cancellation does not await a blocked provider", () =>
            {
                using var entered = new ManualResetEventSlim();
                using var release = new ManualResetEventSlim();
                using var finished = new ManualResetEventSlim();
                using var cancellation = new CancellationTokenSource();
                var caller = Environment.CurrentManagedThreadId;
                var workerIsSta = false;
                var service = new SelectedTextService((foreground, token) =>
                {
                    workerIsSta = Thread.CurrentThread.GetApartmentState() == ApartmentState.STA && Environment.CurrentManagedThreadId != caller;
                    entered.Set();
                    try { release.Wait(); token.ThrowIfCancellationRequested(); return "text"; }
                    finally { finished.Set(); }
                });
                var pending = service.ReadAsync(new IntPtr(1), cancellation.Token);
                try
                {
                    Check(entered.Wait(TimeSpan.FromSeconds(3)) && workerIsSta && !pending.IsCompleted);
                    cancellation.Cancel();
                    Check(SpinWait.SpinUntil(() => pending.IsCompleted, TimeSpan.FromSeconds(3)));
                    Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
                    Check(!finished.IsSet);
                }
                finally { release.Set(); Check(finished.Wait(TimeSpan.FromSeconds(3))); }
            });
            Test("Provider switching retains independent drafts and reasoning choices", () =>
            {
                var original = new AppSettings { TranslationModel = "gpt-5.2", TranslationApiKey = "openai-draft-secret", ReasoningEffort = "high" };
                var window = new SettingsPage(original);
                var provider = (System.Windows.Controls.ComboBox)window.FindName("ProviderBox");
                var key = (System.Windows.Controls.PasswordBox)window.FindName("ApiKeyBox");
                var model = (System.Windows.Controls.ComboBox)window.FindName("ModelBox");
                var reasoning = (System.Windows.Controls.ComboBox)window.FindName("ReasoningBox");
                Check((string?)reasoning.SelectedValue == "high");
                provider.SelectedValue = "deepseek";
                Check(key.Password == "" && ((System.Windows.Controls.TextBox)window.FindName("BaseUrlBox")).Text == "https://api.deepseek.com");
                key.Password = "deepseek-draft-secret";
                model.ItemsSource = new[] { "deepseek-flash" }; model.SelectedItem = "deepseek-flash";
                reasoning.SelectedValue = "max";
                provider.SelectedValue = "openai";
                Check(key.Password == "openai-draft-secret" && (string?)model.SelectedItem == "gpt-5.2" && (string?)reasoning.SelectedValue == "high");
                provider.SelectedValue = "custom";
                Check(key.Password == "");
                provider.SelectedValue = "deepseek";
                Check(key.Password == "deepseek-draft-secret" && (string?)model.SelectedItem == "deepseek-flash" && (string?)reasoning.SelectedValue == "max");
                var draft = window.ReadSettings();
                Check(draft.TranslationProviderId == "deepseek" && draft.TranslationProfiles["openai"].ApiKey == "openai-draft-secret");
                Check(draft.TranslationProfiles["deepseek"].ReasoningEffort == "max");
                Check(original.TranslationProfiles.Count == 0 && original.TranslationApiKey == "openai-draft-secret");
                model.ItemsSource = new[] { "gpt-4.1-mini" }; provider.SelectedValue = "openai";
                model.ItemsSource = new[] { "gpt-4.1-mini" }; model.SelectedItem = "gpt-4.1-mini";
                Check(!reasoning.IsEnabled && (string?)reasoning.SelectedValue == "default");
                window.Dispose();
            });
            Test("Reasoning choices follow model support and advertised DeepSeek levels", () =>
            {
                static string[] Choices(string provider, string model, string[]? levels = null) => ReasoningOptions.ForModel(provider, model, levels).Select(x => x.Value).ToArray();
                Check(Choices("openai", "gpt-4.1-mini").SequenceEqual(new[] { "default" }));
                Check(Choices("openai", "gpt-5-mini").Contains("minimal") && !Choices("openai", "gpt-5-mini").Contains("none"));
                Check(Choices("openai", "gpt-5.1").Contains("none") && !Choices("openai", "gpt-5.1").Contains("xhigh"));
                Check(Choices("openai", "gpt-6-astra").Contains("max") && !Choices("openai", "gpt-6-astra").Contains("none"));
                Check(Choices("openai", "gpt-5.6-sol").Contains("max"));
                Check(!ModelCatalogService.IsTextChatCandidate("gpt-5.2-pro") && !ModelCatalogService.IsTextChatCandidate("gpt-5.4-pro-2026-03-05"));
                Check(Choices("deepseek", "deepseek-flash").SequenceEqual(new[] { "default", "none", "low", "high", "max" }));
                Check(Choices("deepseek", "future-model", new[] { "low", "high", "unknown" }).SequenceEqual(new[] { "default", "none", "low", "high" }));
            });
            Test("Crop uses physical pixels including negative desktop origins", () =>
            {
                var crop = ScreenCapture.Decode(ScreenCapture.CropPng(CreateImage(), new Rect(-50, 25, 160, 70), new Rect(-100, 0, 600, 180)));
                Check(crop.PixelWidth == 160 && crop.PixelHeight == 70);
            });
            Test("State does not translate placeholders or allow parallel operations", () =>
            {
                var vm = new WorkspaceViewModel(); Check(!vm.CanTranslate && !vm.HasResult);
                vm.OcrText = "test"; Check(vm.CanTranslate && vm.HasResult);
                vm.IsBusy = true; Check(!vm.CanTranslate && !vm.CanRetry && !vm.CanInteract);
                vm.IsTranslation = true; Check(!vm.HasResult);
            });
            Test("Model wrapper cleanup preserves document content", () =>
            {
                Check(OcrTextFormatter.Clean("<md> Hello OCR </md>") == "Hello OCR");
                Check(OcrTextFormatter.Clean("<md>first\nsecond") == "first\nsecond");
                Check(OcrTextFormatter.Clean("Use <md> as a tag") == "Use <md> as a tag");
            });
            Test("Endpoint normalization and unsafe address rejection", () =>
            {
                Check(TranslationService.BuildEndpoint("https://example.com/v1/").AbsoluteUri == "https://example.com/v1/chat/completions");
                Check(TranslationService.BuildEndpoint("https://example.com/v1/chat/completions").AbsoluteUri == "https://example.com/v1/chat/completions");
                foreach (var invalid in new[] { "http://example.com/v1", "https://user:key@example.com", "https://example.com?key=secret", "file:///C:/api" })
                    Throws<InvalidOperationException>(() => TranslationService.BuildEndpoint(invalid));
            });
            LanguageMigrationAsync().GetAwaiter().GetResult();
            LegacyMigrationAsync().GetAwaiter().GetResult();
            SettingsRoundTripAsync().GetAwaiter().GetResult();
            TranslationContractAsync().GetAwaiter().GetResult();
            ModelCatalogContractAsync().GetAwaiter().GetResult();
            ProviderPersistenceAsync().GetAwaiter().GetResult();
            ReasoningContractAsync().GetAwaiter().GetResult();
            ModelStopContractAsync().GetAwaiter().GetResult();
            AutoTranslationWorkflowAsync(ScreenCapture.EncodePng(CreateImage())).GetAwaiter().GetResult();
            if (args.Contains("--integration")) LocalOcrAsync(ScreenCapture.EncodePng(CreateImage())).GetAwaiter().GetResult();
            if (args.Contains("--translation-integration")) TranslationIntegrationAsync().GetAwaiter().GetResult();
            Console.WriteLine($"PASS: {_passed} checks. No desktop UI was shown or automated.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Test(string name, Action body) { body(); _passed++; Console.WriteLine("PASS " + name); }
    private static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    { try { await action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }

    private static async Task LanguageMigrationAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Translate-languages-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, "settings.json"), "{\"TargetLanguage\":\"日本語\",\"QuickTargetLanguage\":\"Deutsch\"}");
            var store = new SettingsStore(folder);
            var settings = await store.LoadAsync();
            Check(settings.SelectionTargetLanguage == "日本語" && settings.WorkspaceTargetLanguage == "Deutsch");
            settings.SelectionTargetLanguage = "Français";
            settings.WorkspaceTargetLanguage = "Español";
            settings.TargetLanguage = "简体中文";
            settings.QuickTargetLanguage = "English";
            await store.SaveAsync(settings);
            var restored = await store.LoadAsync();
            Check(restored.TargetLanguage == "简体中文" && restored.SelectionTargetLanguage == "Français" && restored.WorkspaceTargetLanguage == "Español" && restored.QuickTargetLanguage == "English");
            _passed++; Console.WriteLine("PASS independent language migration and persistence");
        }
        finally
        {
            if (!Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid test folder.");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static async Task LegacyMigrationAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "Translate-migration-" + Guid.NewGuid().ToString("N"));
        try
        {
            var legacy = Path.Combine(root, "Mi OCR Desk");
            var current = Path.Combine(root, "Translate");
            var settings = new AppSettings { TranslationApiKey = "migration-secret", TranslationPrompt = "Keep my naming constraint in {targetLanguage}.", CaptureHotkey = "Ctrl+Shift+F8" };
            await new SettingsStore(legacy).SaveAsync(settings);
            var store = new SettingsStore(current, legacy);
            var migrated = await store.LoadAsync();
            Check(migrated.TranslationApiKey == "migration-secret" && migrated.TranslationPrompt == settings.TranslationPrompt);
            Check(migrated.SelectionTranslateHotkey == "Ctrl+Alt+Y" && migrated.QuickTargetLanguage == "英语");
            var json = await File.ReadAllTextAsync(Path.Combine(current, "settings.json"));
            Check(json.Contains("dpapi:") && !json.Contains("migration-secret"));
            migrated.TranslationPrompt = "new constraint";
            await store.SaveAsync(migrated);
            Check((await store.LoadAsync()).TranslationPrompt == "new constraint");
            Check((await new SettingsStore(legacy).LoadAsync()).TranslationPrompt == settings.TranslationPrompt);
            _passed++; Console.WriteLine("PASS legacy settings migration preserves encrypted profiles and prefers new settings");
        }
        finally
        {
            var expected = Path.GetFullPath(Path.GetTempPath());
            if (!Path.GetFullPath(root).StartsWith(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid test folder.");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
    private sealed class ImmediateTranslator(Func<string, AppSettings, TranslationResult> translate) : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(string text, AppSettings settings, Action<string> progress, CancellationToken token = default)
            => Task.FromResult(translate(text, settings));
    }

    private static async Task SettingsRoundTripAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Translate-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SettingsStore(folder);
            var original = new AppSettings { TranslationApiKey = "synthetic-test-key", CaptureHotkey = "Ctrl+Shift+F8", CaptureTranslateHotkey = "Ctrl+Shift+F9", RuntimeRoot = @"D:\model-test", AutoCopy = true, TargetLanguage = "English", TranslationTimeoutSeconds = 45, StartAtLogin = true, SilentStartup = true, RunInBackground = false };
            await store.SaveAsync(original);
            var json = await File.ReadAllTextAsync(Path.Combine(folder, "settings.json"));
            Check(!json.Contains("synthetic-test-key") && json.Contains("dpapi:"));
            var loaded = await store.LoadAsync();
            Check(loaded.TranslationApiKey == original.TranslationApiKey && loaded.RuntimeRoot == original.RuntimeRoot && loaded.AutoCopy && loaded.TargetLanguage == "English" && loaded.TranslationTimeoutSeconds == 45);
            Check(loaded.StartAtLogin && loaded.SilentStartup && !loaded.RunInBackground);
            Check(loaded.CaptureTranslateHotkey == "Ctrl+Shift+F9");
            await File.WriteAllTextAsync(Path.Combine(folder, "settings.json"), "broken JSON");
            Check((await store.LoadAsync()).CaptureHotkey == "Ctrl+Alt+O");
            _passed++; Console.WriteLine("PASS encrypted settings round-trip and corrupt-file recovery");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    private static async Task ModelStopContractAsync()
    {
        var unloads = 0;
        using var client = new HttpClient(new FakeHandler(async (request, token) =>
        {
            Check(request.RequestUri!.Host == "127.0.0.1" && request.RequestUri.Port == 11435);
            if (request.RequestUri.AbsolutePath == "/api/tags")
                return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { models = new[] { new { name = OcrRuntimeService.ModelName } } }));
            Check(request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath == "/api/generate");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Check(body.RootElement.GetProperty("keep_alive").GetInt32() == 0);
            Check(body.RootElement.GetProperty("model").GetString() == OcrRuntimeService.ModelName);
            Check(body.RootElement.GetProperty("prompt").GetString() == "" && !body.RootElement.GetProperty("stream").GetBoolean());
            unloads++;
            return Json(HttpStatusCode.OK, "{\"done\":true,\"done_reason\":\"unload\"}");
        }));
        await new OcrRuntimeService(client).StopAsync();
        Check(unloads == 1);
        using var offline = new HttpClient(new FakeHandler((_, _) => throw new HttpRequestException("offline")));
        await new OcrRuntimeService(offline).StopAsync();
        using var invalid = new HttpClient(new FakeHandler((request, _) => Task.FromResult(Json(HttpStatusCode.OK,
            request.RequestUri!.AbsolutePath == "/api/tags" ? JsonSerializer.Serialize(new { models = new[] { new { name = OcrRuntimeService.ModelName } } }) : "{\"done\":false}"))));
        await ThrowsAsync<InvalidOperationException>(() => new OcrRuntimeService(invalid).StopAsync());
        _passed++; Console.WriteLine("PASS OCR unload request, offline shutdown and failure reporting");

        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--runtime-child");
        using var child = System.Diagnostics.Process.Start(start)!;
        var childId = child.Id;
        try
        {
            // Even a failed HTTP unload must terminate a process owned by the application.
            await new OcrRuntimeService(invalid, child).StopAsync();
            using var remaining = TryGetProcess(childId);
            Check(remaining is null || remaining.HasExited);
        }
        finally
        {
            using var remaining = TryGetProcess(childId);
            if (remaining is not null && !remaining.HasExited) remaining.Kill(entireProcessTree: true);
        }
        _passed++; Console.WriteLine("PASS owned runtime process terminates even if unload fails");
    }
    private static System.Diagnostics.Process? TryGetProcess(int id)
    { try { return System.Diagnostics.Process.GetProcessById(id); } catch (ArgumentException) { return null; } }

    private static async Task AutoTranslationWorkflowAsync(byte[] png)
    {
        foreach (var scenario in new[] { "auto", "ocr_only", "empty", "ocr_error", "ocr_cancel", "cancel_between", "translation_error", "translation_cancel" })
        {
            using var cancellation = new CancellationTokenSource();
            var source = scenario == "empty" ? "  " : "Hello OCR\n你好";
            var requests = new List<string>();
            using var ocrClient = new HttpClient(new FakeHandler(async (request, token) =>
            {
                if (request.RequestUri!.AbsolutePath == "/api/tags")
                    return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { models = new[] { new { name = OcrRuntimeService.ModelName } } }));
                Check(request.RequestUri.AbsolutePath == "/api/chat");
                requests.Add("ocr");
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                Check(body.RootElement.GetProperty("messages")[0].GetProperty("images")[0].GetString() == Convert.ToBase64String(png));
                if (scenario == "ocr_error") return Json(HttpStatusCode.InternalServerError, "{}");
                if (scenario == "ocr_cancel") { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
                return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { message = new { content = source }, done = true }));
            }));
            using var translationClient = new HttpClient(new FakeHandler(async (request, token) =>
            {
                requests.Add("translate");
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                Check(body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() == source);
                Check(body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString()!.Contains("Français"));
                if (scenario == "translation_error") return Json(HttpStatusCode.Unauthorized, "{}");
                if (scenario == "translation_cancel") { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
                return Json(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":\"Bonjour OCR\"}}]}");
            }));
            var view = new WorkspaceViewModel { TargetLanguage = "Français" };
            if (scenario == "cancel_between") view.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(view.OcrMeta)) cancellation.Cancel(); };
            var workflow = new WorkspaceOperations(view, new OcrRuntimeService(ocrClient), new TranslationService(translationClient));
            var settings = new AppSettings { TranslationApiKey = "synthetic-key", TranslationBaseUrl = "https://example.com/v1", TargetLanguage = "Français" };
            var operation = workflow.RecognizeAsync(png, settings, scenario != "ocr_only", cancellation.Token);
            if (scenario.EndsWith("error")) await ThrowsAsync<InvalidOperationException>(() => operation);
            else if (scenario.Contains("cancel")) await ThrowsAsync<OperationCanceledException>(() => operation);
            else await operation;
            var shouldTranslate = scenario is "auto" or "translation_error" or "translation_cancel";
            Check(requests.SequenceEqual(shouldTranslate ? new[] { "ocr", "translate" } : new[] { "ocr" }));
            Check(view.TranslationText == (scenario == "auto" ? "Bonjour OCR" : ""));
            if (shouldTranslate) Check(view.DisplayMode == ResultDisplayMode.Compare && view.OcrText == source);
        }
        _passed++; Console.WriteLine("PASS screenshot translation sequence, selected language, OCR-only, empty text, errors and cancellation boundaries");
    }

    private static async Task TranslationContractAsync()
    {
        var settings = new AppSettings { TranslationApiKey = "synthetic-test-key", TranslationBaseUrl = "https://example.com/v1", TargetLanguage = "English" };
        using var client = new HttpClient(new FakeHandler(async (request, token) =>
        {
            Check(request.RequestUri!.AbsolutePath == "/v1/chat/completions");
            Check(request.Headers.Authorization?.Parameter == "synthetic-test-key");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Check(body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString()!.Contains("English"));
            Check(body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() == "你好");
            return Json(HttpStatusCode.OK, "{\"model\":\"test-model\",\"choices\":[{\"message\":{\"content\":\"Hello\"},\"finish_reason\":\"stop\"}]}");
        }));
        var result = await new TranslationService(client).TranslateAsync("你好", settings, _ => { });
        Check(result.Content == "Hello" && result.Model == "test-model");
        _passed++; Console.WriteLine("PASS translation request, target-language substitution and response");
        foreach (var response in new[] { Json(HttpStatusCode.Unauthorized, "{}"), Json(HttpStatusCode.OK, "{}"), Json(HttpStatusCode.OK, "{\"choices\":[]}"), Json(HttpStatusCode.OK, "not-json") })
        {
            using var errorClient = new HttpClient(new FakeHandler((_, _) => Task.FromResult(response)));
            await ThrowsAsync<InvalidOperationException>(() => new TranslationService(errorClient).TranslateAsync("test", settings, _ => { }));
        }
        _passed++; Console.WriteLine("PASS translation auth errors and malformed responses");
        using var cancelClient = new HttpClient(new FakeHandler(async (_, token) => { await Task.Delay(10000, token); return Json(HttpStatusCode.OK, "{}"); }));
        using var cancel = new CancellationTokenSource(50);
        await ThrowsAsync<OperationCanceledException>(() => new TranslationService(cancelClient).TranslateAsync("test", settings, _ => { }, cancel.Token));
        _passed++; Console.WriteLine("PASS translation cancellation");
    }
    private static async Task ProviderPersistenceAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Translate-profiles-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(folder);
            var store = new SettingsStore(folder);
            // Simulate the pre-profile schema, including a DPAPI-protected legacy key.
            var encrypted = "dpapi:" + Convert.ToBase64String(System.Security.Cryptography.ProtectedData.Protect(Encoding.UTF8.GetBytes("legacy-openai-secret"), null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
            await File.WriteAllTextAsync(Path.Combine(folder, "settings.json"), JsonSerializer.Serialize(new { TranslationProvider = "OpenAI", TranslationBaseUrl = "https://api.openai.com/v1", TranslationApiKey = encrypted, TranslationModel = "gpt-5.2" }));
            var settings = await store.LoadAsync();
            Check(settings.TranslationProviderId == "openai" && settings.TranslationProfiles["openai"].ApiKey == "legacy-openai-secret");
            settings.ReasoningEffort = "high"; settings.ActivateProvider("deepseek");
            Check(settings.TranslationApiKey == "");
            settings.TranslationApiKey = "deepseek-persist-secret"; settings.TranslationModel = "deepseek-flash"; settings.ReasoningEffort = "max";
            await store.SaveAsync(settings);
            var raw = await File.ReadAllTextAsync(Path.Combine(folder, "settings.json"));
            Check(!raw.Contains("legacy-openai-secret") && !raw.Contains("deepseek-persist-secret"));
            using var json = JsonDocument.Parse(raw);
            foreach (var profile in json.RootElement.GetProperty("TranslationProfiles").EnumerateObject()) Check(profile.Value.GetProperty("ApiKey").GetString()!.StartsWith("dpapi:"));
            var loaded = await store.LoadAsync();
            Check(loaded.TranslationApiKey == "deepseek-persist-secret" && loaded.ReasoningEffort == "max");
            loaded.ActivateProvider("openai");
            Check(loaded.TranslationApiKey == "legacy-openai-secret" && loaded.ReasoningEffort == "high" && loaded.TranslationModel == "gpt-5.2");
            loaded.ActivateProvider("deepseek");
            Check(loaded.TranslationApiKey == "deepseek-persist-secret");
            var clone = loaded.Copy(); clone.TranslationProfiles["openai"].ApiKey = "changed-draft";
            Check(loaded.TranslationProfiles["openai"].ApiKey == "legacy-openai-secret");
            _passed++; Console.WriteLine("PASS legacy key migration, encrypted provider profiles, restart restoration and deep-copy isolation");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    private static async Task ReasoningContractAsync()
    {
        foreach (var provider in new[] { "openai", "deepseek" })
        foreach (var effort in new[] { "default", "none", "high", "max" })
        {
            var settings = new AppSettings(); settings.ActivateProvider(provider);
            settings.TranslationModel = provider == "deepseek" ? "deepseek-flash" : "gpt-6-sol";
            settings.TranslationApiKey = provider + "-synthetic-secret"; settings.ReasoningEffort = effort;
            using var client = new HttpClient(new FakeHandler(async (request, token) =>
            {
                Check(request.RequestUri!.Host == (provider == "deepseek" ? "api.deepseek.com" : "api.openai.com"));
                Check(request.Headers.Authorization?.Parameter == provider + "-synthetic-secret");
                using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                var payload = document.RootElement;
                if (effort == "default") Check(!payload.TryGetProperty("reasoning_effort", out _) && !payload.TryGetProperty("thinking", out _));
                else if (provider == "deepseek")
                {
                    Check(payload.GetProperty("thinking").GetProperty("type").GetString() == (effort == "none" ? "disabled" : "enabled"));
                    if (effort == "none") Check(!payload.TryGetProperty("reasoning_effort", out _));
                    else Check(payload.GetProperty("reasoning_effort").GetString() == effort);
                }
                else Check(payload.GetProperty("reasoning_effort").GetString() == effort && !payload.TryGetProperty("thinking", out _));
                return Json(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":\"Hello\",\"reasoning_content\":\"internal thinking\"},\"finish_reason\":\"stop\"}]}");
            }));
            var result = await new TranslationService(client).TranslateAsync("你好", settings, _ => { });
            Check(result.Content == "Hello");
        }
        using var metadataClient = new HttpClient(new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{\"data\":[{\"id\":\"deepseek-flash\",\"effort\":{\"supported_levels\":[\"low\",\"high\",\"max\"]}}]}"))));
        var deepseek = new AppSettings(); deepseek.ActivateProvider("deepseek"); deepseek.TranslationApiKey = "synthetic-secret";
        var models = await new ModelCatalogService(metadataClient).ListDetailsAsync(deepseek);
        Check(models.Single().ReasoningLevels.SequenceEqual(new[] { "low", "high", "max" }));
        _passed++; Console.WriteLine("PASS OpenAI/DeepSeek reasoning payloads, provider-specific credentials, final-text extraction and model metadata");
    }
    private static async Task ModelCatalogContractAsync()
    {
        var settings = new AppSettings { TranslationApiKey = "synthetic-test-key" };
        const string body = "{\"data\":[{\"id\":\"gpt-4.1-mini\"},{\"id\":\"gpt-4.1-mini\"},{\"id\":\"gpt-4.1\"},{\"id\":\"text-embedding-3-small\"},{\"id\":\"gpt-image-1\"},{\"id\":\"gpt-4o-realtime-preview\"},{\"id\":\"gpt-5-codex\"},{\"id\":\"gpt-5-pro\"},{\"id\":\"gpt-5\"},{\"id\":\"o3\"},{\"id\":\"\"},{\"id\":null},{\"id\":12},null]}";
        using var client = new HttpClient(new FakeHandler((request, _) =>
        {
            Check(request.Method == HttpMethod.Get && request.RequestUri!.AbsoluteUri == "https://api.openai.com/v1/models");
            Check(request.Headers.Authorization?.Parameter == "synthetic-test-key");
            return Task.FromResult(Json(HttpStatusCode.OK, body));
        }));
        var catalog = new ModelCatalogService(client);
        Check((await catalog.ListAsync(settings)).SequenceEqual(new[] { "gpt-4.1", "gpt-4.1-mini", "gpt-5", "gpt-5-pro", "o3" }));
        Check(ModelCatalogService.BuildEndpoint("https://example.com/custom/v1/chat/completions/").AbsoluteUri == "https://example.com/custom/v1/models");
        Check(ModelCatalogService.IsTextChatCandidate("ft:gpt-4.1-mini:account:custom:id"));
        settings.TranslationBaseUrl = "https://example.com/v1";
        using var custom = new HttpClient(new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{\"data\":[{\"id\":\"provider/custom-text-v2\"}]}"))));
        Check((await new ModelCatalogService(custom).ListAsync(settings)).Single() == "provider/custom-text-v2");
        _passed++; Console.WriteLine("PASS model catalog authentication, URL normalization, filtering, deduplication and custom providers");
        foreach (var response in new[] { Json(HttpStatusCode.Unauthorized, "{\"error\":\"synthetic-test-key\"}"), Json(HttpStatusCode.NotFound, "{}"), Json(HttpStatusCode.OK, "{\"models\":[]}"), Json(HttpStatusCode.OK, "not-json") })
        {
            using var errorClient = new HttpClient(new FakeHandler((_, _) => Task.FromResult(response)));
            try { await new ModelCatalogService(errorClient).ListAsync(settings); throw new Exception("Expected error"); }
            catch (InvalidOperationException ex) { Check(!ex.Message.Contains("synthetic-test-key")); }
        }
        using var empty = new HttpClient(new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{\"data\":[]}"))));
        Check((await new ModelCatalogService(empty).ListAsync(settings)).Count == 0);
        using var cancelClient = new HttpClient(new FakeHandler(async (_, token) => { await Task.Delay(10000, token); return Json(HttpStatusCode.OK, "{}"); }));
        using var cancel = new CancellationTokenSource(50);
        await ThrowsAsync<OperationCanceledException>(() => new ModelCatalogService(cancelClient).ListAsync(settings, cancel.Token));
        _passed++; Console.WriteLine("PASS model catalog errors, empty lists, cancellation and secret-safe diagnostics");
    }
    private static async Task TranslationIntegrationAsync()
    {
        var settings = await new SettingsStore().LoadAsync();
        if (string.IsNullOrWhiteSpace(settings.TranslationApiKey)) throw new InvalidOperationException("No saved API key was found. Save app settings before running this opt-in check.");
        var endpoint = TranslationService.BuildEndpoint(settings.TranslationBaseUrl);
        if (endpoint.Host != "api.openai.com") throw new InvalidOperationException("This opt-in OpenAI check requires the saved official OpenAI endpoint.");
        Console.WriteLine("Testing saved OpenAI settings; credentials are never printed.");
        var failed = false;
        try
        {
            var models = await new ModelCatalogService().ListAsync(settings);
            Console.WriteLine($"Model list connected: {models.Count} text chat candidates; saved model present: {models.Contains(settings.TranslationModel)}.");
            _passed++;
        }
        catch (Exception ex) { Console.WriteLine("Model list check failed: " + ex.Message); failed = true; }
        try
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var result = await new TranslationService().TranslateAsync("Hello, world.", settings, _ => { });
            Check(!string.IsNullOrWhiteSpace(result.Content));
            Console.WriteLine($"Translation connected: model={result.Model}, returned {result.Content.Length} characters in {timer.Elapsed.TotalSeconds:0.0}s.");
            _passed++;
        }
        catch (Exception ex) { Console.WriteLine("Translation check failed: " + ex.Message); failed = true; }
        if (failed) throw new InvalidOperationException("One or more live OpenAI checks did not pass; see sanitized diagnostics above.");
    }
    private static async Task LocalOcrAsync(byte[] png)
    {
        var service = new OcrRuntimeService(); var settings = new AppSettings();
        var state = await service.InspectAsync(settings);
        Check(state.Installed);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try
        {
            var partials = 0;
            var result = await service.RecognizeAsync(png, settings, Console.WriteLine, _ => partials++, cancel.Token);
            Check(partials > 0 && result.Content.Contains("OCR", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("Recognized synthetic fixture: " + result.Content);
            await service.StopAsync();
            await AssertModelUnloadedAsync();
            result = await service.RecognizeAsync(png, settings, _ => { }, _ => { }, cancel.Token);
            Check(result.Content.Contains("OCR", StringComparison.OrdinalIgnoreCase));
        }
        finally { await service.StopAsync(); }
        await AssertModelUnloadedAsync();
        _passed++; Console.WriteLine("PASS real local OCR, model unload, automatic reload and final cleanup");
    }
    private static async Task AssertModelUnloadedAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await client.GetAsync(OcrRuntimeService.ServiceUrl + "/api/ps");
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Check(!body.RootElement.GetProperty("models").EnumerateArray().Any(model => model.GetProperty("name").GetString() == OcrRuntimeService.ModelName));
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException) { /* Owned server has exited. */ }
    }
    private static BitmapSource CreateImage()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 600, 180));
            dc.DrawText(new FormattedText("Hello Xiaomi OCR\nWindows desktop test 2026", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 30, Brushes.Black, 1), new Point(26, 38));
        }
        var bitmap = new RenderTargetBitmap(600, 180, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); bitmap.Freeze(); return bitmap;
    }
    private static HttpResponseMessage Json(HttpStatusCode code, string body) => new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token); }
}
