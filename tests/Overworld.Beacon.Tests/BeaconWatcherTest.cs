using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Overworld.Beacon.Tests;

public sealed class BeaconWatcherTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("beacon-").FullName;

    private string Path => System.IO.Path.Combine(_folder, "overworld-beacon.sock");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static async Task<string> NextAsync(Beam beam, IAsyncEnumerator<string> watcher, string signal)
    {
        var next = watcher.MoveNextAsync().AsTask();

        while (!next.IsCompleted)
        {
            beam.Light(signal);
            await Task.WhenAny(next, Task.Delay(20, Token));
        }

        Assert.True(await next);

        return watcher.Current;
    }

    [Fact]
    public async Task ReadsTheSignalsTheBeaconSends()
    {
        var beam = new Beam();
        using var server = new BeaconServer(Path, beam, NullLogger<BeaconServer>.Instance);
        server.Start();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var shining = server.RunAsync(stop.Token);
        await using var watcher = new BeaconWatcher(Path).WatchAsync(stop.Token).GetAsyncEnumerator(stop.Token);

        Assert.Equal("theme", await NextAsync(beam, watcher, "theme"));

        await stop.CancelAsync();
        await shining;
    }

    [Fact]
    public async Task WaitsForABeaconThatIsNotShiningYet()
    {
        var beam = new Beam();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(Token);
        await using var watcher = new BeaconWatcher(Path, TimeSpan.FromMilliseconds(20)).WatchAsync(stop.Token).GetAsyncEnumerator(stop.Token);
        var waiting = NextAsync(beam, watcher, "pins");
        await Task.Delay(100, Token);

        using var server = new BeaconServer(Path, beam, NullLogger<BeaconServer>.Instance);
        server.Start();
        var shining = server.RunAsync(stop.Token);

        Assert.Equal("pins", await waiting);

        await stop.CancelAsync();
        await shining;
    }

    [Fact]
    public async Task SkipsLinesThatAreNotSignals()
    {
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(Path));
        listener.Listen();
        await using var watcher = new BeaconWatcher(Path).WatchAsync(Token).GetAsyncEnumerator(Token);
        var next = watcher.MoveNextAsync().AsTask();

        using var client = await listener.AcceptAsync(Token);
        await using var writer = new StreamWriter(new NetworkStream(client)) { AutoFlush = true };
        await writer.WriteLineAsync("not json".AsMemory(), Token);
        await writer.WriteLineAsync("{\"signal\":3}".AsMemory(), Token);
        await writer.WriteLineAsync("{\"signal\":\"theme\"}".AsMemory(), Token);

        Assert.True(await next);
        Assert.Equal("theme", watcher.Current);
    }
}
