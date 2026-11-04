namespace Translate;

/// <summary>Owns the window's shortcuts and restores the previous registrations if a change fails.</summary>
internal sealed class HotkeyRegistry(Func<int, uint, uint, bool> register, Action<int> unregister)
{
    internal static IEnumerable<(int Id, string Text, string Label)> Shortcuts(AppSettings settings)
    {
        yield return (NativeMethods.CaptureHotkeyId, settings.CaptureHotkey, "截图");
        yield return (NativeMethods.CaptureTranslateHotkeyId, settings.CaptureTranslateHotkey, "截图并翻译");
        yield return (NativeMethods.SelectionTranslateHotkeyId, settings.SelectionTranslateHotkey, "划词翻译");
        yield return (NativeMethods.QuickTranslateHotkeyId, settings.QuickTranslateHotkey, "输入翻译");
    }
    internal void Apply(AppSettings settings)
    {
        HotkeyParser.ValidateAll(settings);
        var previous = Snapshot();
        Clear();
        foreach (var shortcut in Shortcuts(settings))
        {
            if (TryAdd(shortcut.Id, shortcut.Text)) continue;
            var restored = Restore(previous);
            throw new InvalidOperationException(shortcut.Label + "快捷键已被占用。" + (restored ? "原快捷键已恢复，设置未保存。" : "请重新设置快捷键。"));
        }
    }
    private readonly Dictionary<int, string> _registered = new();
    internal Dictionary<int, string> Snapshot() => new(_registered);
    internal bool TryAdd(int id, string text)
    {
        if (!HotkeyParser.TryParse(text, out var modifiers, out var key)) return false;
        if (_registered.Any(pair => HotkeyParser.TryParse(pair.Value, out var otherModifiers, out var otherKey) && otherModifiers == modifiers && otherKey == key)) return false;
        if (!register(id, modifiers | 0x4000, key)) return false;
        _registered[id] = text;
        return true;
    }
    internal void Apply(string capture, string translate)
    {
        HotkeyParser.ValidatePair(capture, translate);
        var previous = Snapshot();
        Clear();
        string? error = null;
        if (!TryAdd(NativeMethods.CaptureHotkeyId, capture)) error = "截图快捷键已被占用，请换一个组合键。";
        else if (!TryAdd(NativeMethods.CaptureTranslateHotkeyId, translate)) error = "截图并翻译快捷键已被占用，请换一个组合键。";
        if (error is null) return;
        var restored = Restore(previous);
        throw new InvalidOperationException(error + (restored ? "原快捷键继续有效，设置未保存。" : "部分原快捷键也被占用，请重新设置。"));
    }
    internal bool Restore(IReadOnlyDictionary<int, string> registrations)
    {
        Clear();
        var restored = true;
        foreach (var pair in registrations) restored &= TryAdd(pair.Key, pair.Value);
        return restored;
    }
    internal void Clear()
    {
        foreach (var id in _registered.Keys) unregister(id);
        _registered.Clear();
    }
}
