using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Bedrock.Operations;
using Overworld.Redstone;

namespace Overworld.Beacon.Tests;

public sealed class BeaconClientTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("beacon-").FullName;

    private string Socket => Path.Combine(_folder, "overworld.sock");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task LightsASignalOnTheDaemonsBeam()
    {
        var beam = new Beam();
        using var server = new RedstoneServer(
            Socket,
            new Dispatcher([Operation.From(new LightBeaconOp(beam))], NullLogger<Dispatcher>.Instance),
            NullLogger<RedstoneServer>.Instance);
        server.Start();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var serving = server.RunAsync(stop.Token);
        await using var watcher = beam.WatchAsync(Token).GetAsyncEnumerator(Token);
        var next = watcher.MoveNextAsync();

        var lit = await new BeaconClient(new RedstoneClient(Socket)).LightAsync("theme", Token);

        Assert.Equal("theme", lit.Value);
        Assert.True(await next);
        Assert.Equal("theme", watcher.Current);

        await stop.CancelAsync();
        await serving;
    }

    [Fact]
    public async Task TellsWhenTheDaemonIsNotRunning()
    {
        var lit = await new BeaconClient(new RedstoneClient(Socket)).LightAsync("theme", Token);

        Assert.Equal("daemon.not_running", lit.Error.Code);
    }
}
