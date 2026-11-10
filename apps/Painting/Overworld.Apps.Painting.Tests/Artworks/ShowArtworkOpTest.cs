using Overworld.Apps.Painting.Artworks;

namespace Overworld.Apps.Painting.Tests.Artworks;

public sealed class ShowArtworkOpTest : IDisposable
{
    private static readonly DateTimeOffset Morning = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);

    private readonly TemporaryFolder _folder = new();

    private readonly ArtworkStore _store;

    private readonly ShowArtworkOp _show;

    public ShowArtworkOpTest()
    {
        _store = new ArtworkStore(_folder.Path);
        _show = new ShowArtworkOp(_store);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task ShowsTheLatestVersionOfThePaintingPaintedLast()
    {
        var later = await Add("Later", Morning.AddHours(2));
        await Add("Earlier", Morning);

        var shown = (await _show.ExecuteAsync(new ShowArtwork(), Token)).Value;

        Assert.Equal((later.Id, "<h1>Later</h1>", 1), (shown.Id, shown.Html, shown.Version));
    }

    [Fact]
    public async Task ShowsAPaintingByItsId()
    {
        var earlier = await Add("Earlier", Morning);
        await Add("Later", Morning.AddHours(2));

        Assert.Equal("<h1>Earlier</h1>", (await _show.ExecuteAsync(new ShowArtwork(earlier.Id), Token)).Value.Html);
    }

    [Fact]
    public async Task ShowsAnEarlierVersion()
    {
        var artwork = await Add("Design", Morning);
        await _store.ChangeAsync(artwork.Id, kept => kept.Painted("<h1>Second</h1>", Morning.AddHours(1)), Token);

        var shown = (await _show.ExecuteAsync(new ShowArtwork(artwork.Id, 1), Token)).Value;
        var latest = (await _show.ExecuteAsync(new ShowArtwork(artwork.Id), Token)).Value;

        Assert.Equal(("<h1>Design</h1>", 1, 2, Morning), (shown.Html, shown.Version, shown.Versions, shown.Painted));
        Assert.Equal(("<h1>Second</h1>", 2), (latest.Html, latest.Version));
    }

    [Fact]
    public async Task SaysWhenThereIsNoSuchVersion()
    {
        var artwork = await Add("Design", Morning);

        var shown = await _show.ExecuteAsync(new ShowArtwork(artwork.Id, 4), Token);

        Assert.Equal("painting.version_not_found", shown.Error.Code);
    }

    [Fact]
    public async Task SaysNothingHasBeenPaintedYet()
    {
        var shown = await _show.ExecuteAsync(new ShowArtwork(), Token);

        Assert.Equal(ShowArtworkOp.Empty, shown.Error.Code);
    }

    [Fact]
    public async Task SaysWhenThereIsNoPaintingWithTheId()
    {
        await Add("Design", Morning);

        var shown = await _show.ExecuteAsync(new ShowArtwork(Guid.NewGuid()), Token);

        Assert.Equal("painting.not_found", shown.Error.Code);
    }

    private async Task<Artwork> Add(string title, DateTimeOffset painted) =>
        (await _store.AddAsync(Artwork.New(title, null, $"<h1>{title}</h1>", painted), Token)).Value;
}
