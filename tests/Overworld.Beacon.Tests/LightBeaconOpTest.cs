namespace Overworld.Beacon.Tests;

public sealed class LightBeaconOpTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LightsTheSignalOnTheBeam()
    {
        var beam = new Beam();
        await using var watcher = beam.WatchAsync(Token).GetAsyncEnumerator(Token);
        var next = watcher.MoveNextAsync();

        var lit = await new LightBeaconOp(beam).ExecuteAsync(new LightBeacon(" theme "), Token);

        Assert.Equal("theme", lit.Value);
        Assert.True(await next);
        Assert.Equal("theme", watcher.Current);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefusesASignalWithoutAName(string signal)
    {
        var lit = await new LightBeaconOp(new Beam()).ExecuteAsync(new LightBeacon(signal), Token);

        Assert.Equal("beacon.signal_empty", lit.Error.Code);
    }
}
