using Overworld.Apps.Painting.Artworks;
using Overworld.Beacon;
using Overworld.Redstone;

namespace Overworld.Apps.Painting.Tests.Artworks;

public sealed class GroupArtworkOpTest : IDisposable
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

    private readonly DateTimeOffset _now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly ArtworkStore _store;

    private readonly GroupArtworkOp _group;

    public GroupArtworkOpTest()
    {
        _store = new ArtworkStore(_folder.Path);
        _group = new GroupArtworkOp(_store, new BeaconClient(_daemon));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task PutsAPaintingInAGroupWithoutANewVersion()
    {
        var artwork = (await _store.AddAsync(Artwork.New("Old mockup", null, "<p>one</p>", _now), Token)).Value;

        var grouped = await _group.ExecuteAsync(new GroupArtwork(artwork.Id, ArtworkGroup.Design), Token);

        Assert.Equal((ArtworkGroup.Design, 1), (grouped.Value.Group, grouped.Value.Versions));
        Assert.Equal(ArtworkGroup.Design, (await _store.FindAsync(artwork.Id, Token)).Value.Group);
        Assert.Equal("painting", Assert.Single(_daemon.Requests).Input!["signal"]!.GetValue<string>());
    }

    [Fact]
    public async Task TakesAPaintingOutOfItsGroup()
    {
        var artwork = (await _store.AddAsync(Artwork.New("Mockup", ArtworkGroup.Design, "<p>one</p>", _now), Token)).Value;

        var grouped = await _group.ExecuteAsync(new GroupArtwork(artwork.Id), Token);

        Assert.Null(grouped.Value.Group);
    }

    [Fact]
    public async Task RefusesAPaintingThatIsNotThere()
    {
        var grouped = await _group.ExecuteAsync(new GroupArtwork(Guid.NewGuid(), ArtworkGroup.Design), Token);

        Assert.Equal("painting.not_found", grouped.Error.Code);
        Assert.Empty(_daemon.Requests);
    }
}
