using Overworld.Apps.Painting.Artworks;
using Overworld.Beacon;
using Overworld.Redstone;

namespace Overworld.Apps.Painting.Tests.Artworks;

public sealed class RestoreArtworkOpTest : IDisposable
{
    private sealed class Daemon : IRedstoneClient
    {
        public List<Request> Requests { get; } = [];

        public Task<Response> SendAsync(Request request, CancellationToken token)
        {
            Requests.Add(request);

            return Task.FromResult(Response.Success(null));
        }
    }

    private readonly TemporaryFolder _folder = new();

    private readonly Daemon _daemon = new();

    private readonly Clock _clock = new(new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero));

    private readonly ArtworkStore _store;

    private readonly RestoreArtworkOp _restore;

    public RestoreArtworkOpTest()
    {
        _store = new ArtworkStore(_folder.Path);
        _restore = new RestoreArtworkOp(_store, new BeaconClient(_daemon), _clock);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task GoesBackByPaintingAnEarlierVersionAgainAsTheNewest()
    {
        var artwork = await TwoVersions();

        var restored = await _restore.ExecuteAsync(new RestoreArtwork(artwork.Id, 1), Token);

        Assert.Equal((3, 3, _clock.Now), (restored.Value.Version, restored.Value.Versions, restored.Value.Painted));
        var kept = (await _store.FindAsync(artwork.Id, Token)).Value;
        Assert.Equal(["<p>one</p>", "<p>two</p>", "<p>one</p>"], kept.Versions.Select(version => version.Html));
        Assert.Equal("painting", Assert.Single(_daemon.Requests).Input!["signal"]!.GetValue<string>());
    }

    [Fact]
    public async Task RefusesAVersionThatIsNotThere()
    {
        var artwork = await TwoVersions();

        var restored = await _restore.ExecuteAsync(new RestoreArtwork(artwork.Id, 7), Token);

        Assert.Equal("painting.version_not_found", restored.Error.Code);
        Assert.Empty(_daemon.Requests);
    }

    [Fact]
    public async Task RefusesAPaintingThatIsNotThere()
    {
        var restored = await _restore.ExecuteAsync(new RestoreArtwork(Guid.NewGuid(), 1), Token);

        Assert.Equal("painting.not_found", restored.Error.Code);
    }

    private async Task<Artwork> TwoVersions()
    {
        var artwork = (await _store.AddAsync(Artwork.New("Design", null, "<p>one</p>", _clock.Now), Token)).Value;

        return (await _store.ChangeAsync(artwork.Id, kept => kept.Painted("<p>two</p>", _clock.Now), Token)).Value;
    }
}
