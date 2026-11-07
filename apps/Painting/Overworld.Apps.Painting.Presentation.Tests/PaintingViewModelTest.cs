using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Painting.Presentation.Tests;

public sealed class PaintingViewModelTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("painting-").FullName;

    private readonly ArtworkStore _store;

    private readonly PaintingViewModel _painting;

    public PaintingViewModelTest()
    {
        _store = new ArtworkStore(_folder);
        _painting = new PaintingViewModel(Operation.From(new ShowArtworkOp(_store)));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task IsEmptyWhenNothingHasBeenPainted()
    {
        await _painting.LoadAsync(Token);

        Assert.True(_painting.IsEmpty);
        Assert.Null(_painting.Artwork);
        Assert.Null(_painting.Problem);
    }

    [Fact]
    public async Task ShowsThePaintingPaintedLast()
    {
        var artwork = new Artwork(Guid.NewGuid(), "Design", "<h1>Hi</h1>", DateTimeOffset.UnixEpoch);
        await _store.AddAsync(artwork, Token);

        await _painting.LoadAsync(Token);

        Assert.False(_painting.IsEmpty);
        Assert.Equal("Design", _painting.Title);
        Assert.Equal("<h1>Hi</h1>", _painting.Html);
    }

    [Fact]
    public async Task SaysWhatWentWrongWhenThePaintingsCannotBeRead()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "paintings.json"), "{ not json", Token);

        await _painting.LoadAsync(Token);

        Assert.False(_painting.IsEmpty);
        Assert.NotNull(_painting.Problem);
    }
}
