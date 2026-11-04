using System.Runtime.InteropServices;

namespace MiOcrDesk;

internal static class NativeMethods
{
    internal const int WM_HOTKEY = 0x0312;
    internal const int CaptureHotkeyId = 0x4D49;
    internal const int CaptureTranslateHotkeyId = 0x4D4A;
    internal const uint MOD_ALT = 0x0001;
    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;
    internal const uint MOD_WIN = 0x0008;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [StructLayout(LayoutKind.Sequential)] internal struct NativePoint { public int X; public int Y; }
    internal static NativePoint CursorPosition { get { GetCursorPos(out var point); return point; } }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    internal static void TryRoundCorners(IntPtr hwnd) { try { var preference = 2; DwmSetWindowAttribute(hwnd, 33, ref preference, sizeof(int)); } catch (DllNotFoundException) { } }
}

internal static class HotkeyParser
{
    internal static void ValidatePair(string capture, string translate)
    {
        if (!TryParse(capture, out var captureModifiers, out var captureKey)) throw new InvalidOperationException("截图快捷键格式无效，例如 Ctrl+Alt+O。");
        if (!TryParse(translate, out var translateModifiers, out var translateKey)) throw new InvalidOperationException("截图并翻译快捷键格式无效，例如 Ctrl+Alt+T。");
        if (captureModifiers == translateModifiers && captureKey == translateKey) throw new InvalidOperationException("截图和截图并翻译不能使用同一个快捷键。");
    }
    internal static bool TryParse(string input, out uint modifiers, out uint key)
    {
        modifiers = 0; key = 0;
        foreach (var item in input.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (item.ToUpperInvariant())
            {
                case "CTRL": case "CONTROL": modifiers |= NativeMethods.MOD_CONTROL; break;
                case "ALT": modifiers |= NativeMethods.MOD_ALT; break;
                case "SHIFT": modifiers |= NativeMethods.MOD_SHIFT; break;
                case "WIN": case "WINDOWS": modifiers |= NativeMethods.MOD_WIN; break;
                default:
                    if (key != 0 || !TryKey(item, out key)) return false;
                    break;
            }
        }
        return modifiers != 0 && key != 0;
    }
    private static bool TryKey(string input, out uint key)
    {
        key = 0;
        input = input.ToUpperInvariant();
        if (input.Length == 1 && (input[0] is >= 'A' and <= 'Z' or >= '0' and <= '9')) { key = input[0]; return true; }
        if (input.Length is 2 or 3 && input.StartsWith('F') && int.TryParse(input[1..], out var f) && f is >= 1 and <= 24) { key = (uint)(0x70 + f - 1); return true; }
        return false;
    }
}
