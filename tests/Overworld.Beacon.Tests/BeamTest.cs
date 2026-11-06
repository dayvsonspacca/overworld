namespace Overworld.Beacon.Tests;

public sealed class BeamTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EveryWatcherSeesTheSignal()
    {
        var beam = new Beam();
        await using var first = beam.WatchAsync(Token).GetAsyncEnumerator(Token);
        await using var second = beam.WatchAsync(Token).GetAsyncEnumerator(Token);
        var firstNext = first.MoveNextAsync();
        var secondNext = second.MoveNextAsync();

        beam.Light("theme");

        Assert.True(await firstNext);
        Assert.True(await secondNext);
        Assert.Equal("theme", first.Current);
        Assert.Equal("theme", second.Current);
    }

    [Fact]
    public async Task AWatcherSeesSignalsInTheOrderTheyWereLit()
    {
        var beam = new Beam();
        await using var watcher = beam.WatchAsync(Token).GetAsyncEnumerator(Token);
        var next = watcher.MoveNextAsync();

        beam.Light("pins");
        beam.Light("theme");

        Assert.True(await next);
        Assert.Equal("pins", watcher.Current);
        Assert.True(await watcher.MoveNextAsync());
        Assert.Equal("theme", watcher.Current);
    }

    [Fact]
    public async Task ASlowWatcherKeepsOnlyTheNewestSignals()
    {
        var beam = new Beam();
        await using var watcher = beam.WatchAsync(Token).GetAsyncEnumerator(Token);
        var next = watcher.MoveNextAsync();

        foreach (var number in Enumerable.Range(0, 40))
        {
            beam.Light($"signal-{number}");
        }

        List<string> seen = [];

        while (await next)
        {
            seen.Add(watcher.Current);

            if (watcher.Current == "signal-39")
            {
                break;
            }

            next = watcher.MoveNextAsync();
        }

        Assert.Equal("signal-39", seen[^1]);
        Assert.True(seen.Count <= 17);
    }

    [Fact]
    public void LightingWithNobodyWatchingDoesNothing() => new Beam().Light("theme");

    [Fact]
    public async Task AWatcherThatStopsNoLongerHolds()
    {
        var beam = new Beam();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var watcher = beam.WatchAsync(stop.Token).GetAsyncEnumerator(stop.Token);
        var next = watcher.MoveNextAsync();

        await stop.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await next);
        await watcher.DisposeAsync();
        beam.Light("theme");
    }
}
