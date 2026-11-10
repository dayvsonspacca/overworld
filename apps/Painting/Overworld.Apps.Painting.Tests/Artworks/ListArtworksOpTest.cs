using Overworld.Apps.Painting.Artworks;

namespace Overworld.Apps.Painting.Tests.Artworks;

public sealed class ListArtworksOpTest : IDisposable
{
    private static readonly DateTimeOffset Morning = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);

    private readonly TemporaryFolder _folder = new();

    private readonly ArtworkStore _store;

    private readonly ListArtworksOp _list;

    public ListArtworksOpTest()
    {
        _store = new ArtworkStore(_folder.Path);
        _list = new ListArtworksOp(_store);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task ListsThePaintingPaintedLastFirstWithoutTheirPages()
    {
        var buttons = await Add("Buttons", ArtworkGroup.Design, Morning);
        await Add("Notes", null, Morning.AddHours(1));
        await _store.ChangeAsync(buttons.Id, artwork => artwork.Painted("<p>again</p>", Morning.AddHours(2)), Token);

        var listed = (await _list.ExecuteAsync(new ListArtworks(), Token)).Value;

        Assert.Equal(["Buttons", "Notes"], listed.Select(canvas => canvas.Title));
        Assert.Equal((2, 2), (listed[0].Version, listed[0].Versions));
        Assert.All(listed, canvas => Assert.Null(canvas.Html));
    }

    [Fact]
    public async Task ListsOnlyTheGroupAskedFor()
    {
        await Add("Buttons", ArtworkGroup.Design, Morning);
        await Add("Notes", null, Morning.AddHours(1));

        var listed = (await _list.ExecuteAsync(new ListArtworks(ArtworkGroup.Design), Token)).Value;

        Assert.Equal("Buttons", Assert.Single(listed).Title);
    }

    private async Task<Artwork> Add(string title, ArtworkGroup? group, DateTimeOffset painted) =>
        (await _store.AddAsync(Artwork.New(title, group, $"<h1>{title}</h1>", painted), Token)).Value;
}
