using System.IO.Pipes;
using System.Security.Cryptography;

namespace Translate;

internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _stop = new();
    private readonly string _name;
    internal bool IsPrimary { get; }
    internal SingleInstance(string? isolatedScope = null)
    {
        // Session-specific: each signed-in desktop has its own hotkey and tray icon.
        var user = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserDomainName + "\\" + Environment.UserName)))[..16];
        _name = $"MiOcrDesk-{user}-{System.Diagnostics.Process.GetCurrentProcess().SessionId}" + (isolatedScope is null ? "" : "-" + isolatedScope);
        _mutex = new Mutex(true, @"Local\" + _name, out var primary);
        IsPrimary = primary;
    }
    internal async Task SignalAsync(string command)
    {
        using var client = new NamedPipeClientStream(".", _name, PipeDirection.Out, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.ConnectAsync(timeout.Token);
        using var writer = new StreamWriter(client) { AutoFlush = true };
        await writer.WriteLineAsync(command.AsMemory(), timeout.Token);
    }
    internal async Task ListenAsync(Action<string> received)
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(_name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(_stop.Token);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                using var reader = new StreamReader(server);
                var command = await reader.ReadLineAsync(timeout.Token);
                if (command is "show" or "capture") received(command);
            }
            catch (OperationCanceledException) { }
            catch (IOException) { if (!_stop.IsCancellationRequested) await Task.Delay(250); }
        }
    }
    public void Dispose() { _stop.Cancel(); if (IsPrimary) _mutex.ReleaseMutex(); _mutex.Dispose(); }
}
