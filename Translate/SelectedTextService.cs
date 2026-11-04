using System.Runtime.InteropServices;

namespace Translate;

internal sealed class SelectedTextService
{
    private static int _clipboardBusy;
    private readonly Func<IntPtr, CancellationToken, string> _read;
    internal SelectedTextService(Func<IntPtr, CancellationToken, string>? read = null) => _read = read ?? ReadCore;
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);

    // INPUT is 40 bytes on x64. The union includes mouse data to preserve native alignment.
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputData Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputData
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    { public ushort Key, Scan; public uint Flags, Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput
    { public int X, Y; public uint MouseData, Flags, Time; public UIntPtr ExtraInfo; }

    internal async Task<string> ReadAsync(IntPtr foreground, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _clipboardBusy, 1, 0) != 0)
            throw new InvalidOperationException("上一次文字读取仍在结束，请稍后重试。");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(6));
        var requestToken = timeout.Token;
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        // Clipboard providers may block in native code. Keep their STA calls off the UI thread.
        var worker = new Thread(() =>
        {
            try { completion.TrySetResult(_read(foreground, requestToken)); }
            catch (OperationCanceledException) { completion.TrySetCanceled(requestToken); }
            catch (Exception ex)
            {
                if (requestToken.IsCancellationRequested) completion.TrySetCanceled(requestToken);
                else completion.TrySetException(ex);
            }
            finally { Volatile.Write(ref _clipboardBusy, 0); }
        }) { IsBackground = true, Name = "Translate selection clipboard" };
        try { worker.SetApartmentState(ApartmentState.STA); worker.Start(); }
        catch { Volatile.Write(ref _clipboardBusy, 0); throw; }
        try { return await completion.Task.WaitAsync(requestToken); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new TimeoutException("读取选中文字超时，请复制文字后粘贴到翻译窗口。"); }
    }
    private static string ReadCore(IntPtr foreground, CancellationToken token)
    {
        if (foreground == IntPtr.Zero) throw new InvalidOperationException("请先在 PDF 或其他应用中选中文字。");
        // Wait for the physical shortcut keys to be released before sending Ctrl+C.
        var modifiers = new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C };
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (modifiers.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0))
        {
            token.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= deadline) throw new InvalidOperationException("请松开快捷键后重试。");
            Thread.Sleep(30);
        }
        token.ThrowIfCancellationRequested();
        EnsureForeground(foreground);
        var previous = Clipboard.GetDataObject();
        token.ThrowIfCancellationRequested();
        EnsureForeground(foreground);
        var before = GetClipboardSequenceNumber();
        uint copied = 0;
        try
        {
            var inputs = new[] { Key(0x11), Key(0x43), Key(0x43, true), Key(0x11, true) };
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != inputs.Length)
            {
                if (sent > 0)
                {
                    var release = new[] { Key(0x43, true), Key(0x11, true) };
                    SendInput((uint)release.Length, release, Marshal.SizeOf<Input>());
                }
                throw new InvalidOperationException("无法复制所选文字，请检查目标应用权限，或复制后粘贴到翻译窗口。");
            }
            deadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(40);
                token.ThrowIfCancellationRequested();
                EnsureForeground(foreground);
                if (GetClipboardSequenceNumber() == before) continue;
                try
                {
                    var text = Clipboard.ContainsText() ? Clipboard.GetText() : "";
                    copied = GetClipboardSequenceNumber();
                    token.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(text)) break;
                    return text;
                }
                catch (COMException) { /* The source application may still own the clipboard lock. */ }
            }
            throw new InvalidOperationException("未读到所选文字。请确认文字可复制，或将文字粘贴到输入框后翻译。");
        }
        finally
        {
            // Restore only while the clipboard still contains our copy, never a later user copy.
            if (copied != 0 && GetClipboardSequenceNumber() == copied)
            {
                try { if (previous is null) Clipboard.Clear(); else Clipboard.SetDataObject(previous, true); }
                catch (Exception ex) when (ex is ExternalException or InvalidOperationException or ArgumentException) { }
            }
        }
    }
    private static Input Key(ushort key, bool release = false) => new()
    { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key, Flags = release ? 2u : 0u } } };
    private static void EnsureForeground(IntPtr expected)
    {
        if (GetForegroundWindow() != expected) throw new InvalidOperationException("当前窗口已切换，请重新选中文字后按快捷键。");
    }
}
