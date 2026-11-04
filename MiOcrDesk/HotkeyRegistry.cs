namespace MiOcrDesk;

/// <summary>Owns the window's shortcuts and restores the previous registrations if a change fails.</summary>
internal sealed class HotkeyRegistry(Func<int, uint, uint, bool> register, Action<int> unregister)
{
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
