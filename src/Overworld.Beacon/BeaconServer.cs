using System.Net.Sockets;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Overworld.Beacon;

public sealed partial class BeaconServer(string path, Beam beam, ILogger<BeaconServer> logger) : IDisposable
{
    public const string SocketName = "overworld-beacon";

    private Socket? _listener;

    public void Start()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            listener.Bind(new UnixDomainSocketEndPoint(path));
            listener.Listen();
        }
        catch
        {
            listener.Dispose();
            throw;
        }

        _listener = listener;
        LogShining(path);
    }

    public async Task RunAsync(CancellationToken token)
    {
        var listener = _listener ?? throw new InvalidOperationException("Start the server before running it.");

        try
        {
            while (true)
            {
                var watcher = await listener.AcceptAsync(token);
                _ = ShineAsync(watcher, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (_listener is null)
        {
            return;
        }

        _listener.Dispose();
        File.Delete(path);
    }

    private async Task ShineAsync(Socket watcher, CancellationToken token)
    {
        await using var stream = new NetworkStream(watcher, ownsSocket: true);
        await using var writer = new StreamWriter(stream) { AutoFlush = true };

        try
        {
            await foreach (var signal in beam.WatchAsync(token))
            {
                var line = new JsonObject { ["signal"] = signal }.ToJsonString();
                await writer.WriteLineAsync(line.AsMemory(), token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
            LogWatcherLeft();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Shining on {Path}")]
    private partial void LogShining(string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A watcher left the beacon")]
    private partial void LogWatcherLeft();
}
