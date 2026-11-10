using Overworld.Apps.Painting.Artworks;

namespace Overworld.Apps.Painting.Tests.Artworks;

public sealed class ArtworkStoreTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task KeepsEachPaintingInItsOwnFile()
    {
        var store = new ArtworkStore(_folder.Path);

        var artwork = (await store.AddAsync(Artwork.New("Design", ArtworkGroup.Design, "<p></p>", DateTimeOffset.UnixEpoch), Token)).Value;

        Assert.True(File.Exists(Path.Combine(_folder.Path, "paintings", $"{artwork.Id}.json")));
        var found = (await new ArtworkStore(_folder.Path).FindAsync(artwork.Id, Token)).Value;
        Assert.Equal((artwork.Id, "Design", (ArtworkGroup?)ArtworkGroup.Design), (found.Id, found.Title, found.Group));
        Assert.Equal(artwork.Versions, found.Versions);
    }

    [Fact]
    public async Task MovesPaintingsKeptTheOldWayIntoTheirOwnFilesAsTheirFirstVersion()
    {
        var id = Guid.NewGuid();
        await File.WriteAllTextAsync(
            Path.Combine(_folder.Path, "paintings.json"),
            $$"""[{ "id": "{{id}}", "title": "Old", "html": "<p>old</p>", "painted": "2026-11-07T12:00:00+00:00" }]""",
            Token);

        var listed = (await new ArtworkStore(_folder.Path).ListAsync(Token)).Value;

        var artwork = Assert.Single(listed);
        Assert.Equal((id, "Old", (ArtworkGroup?)null), (artwork.Id, artwork.Title, artwork.Group));
        var version = Assert.Single(artwork.Versions);
        Assert.Equal((1, "<p>old</p>"), (version.Number, version.Html));
        Assert.False(File.Exists(Path.Combine(_folder.Path, "paintings.json")));
        Assert.True(File.Exists(Path.Combine(_folder.Path, "paintings.json.migrated")));
    }

    [Fact]
    public async Task SaysThereIsNoPaintingWithAnUnknownId()
    {
        var found = await new ArtworkStore(_folder.Path).FindAsync(Guid.NewGuid(), Token);

        Assert.Equal("painting.not_found", found.Error.Code);
    }
}
