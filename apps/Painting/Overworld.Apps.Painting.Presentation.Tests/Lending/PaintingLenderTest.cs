using System.Text.Json;
using Overworld.Apps.Painting.Artworks;
using Overworld.Apps.Painting.Presentation.Lending;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Painting.Presentation.Tests.Lending;

public sealed class PaintingLenderTest : IDisposable
{
    private static readonly Guid Palette = Guid.Parse("1d9b883c-b335-4dc4-b228-6bf3aeb6802c");

    private readonly string _folder = Directory.CreateTempSubdirectory("painting-").FullName;

    private readonly ArtworkStore _store;

    private readonly PaintingLender _lender;

    public PaintingLenderTest()
    {
        _store = new ArtworkStore(_folder);
        _lender = new PaintingLender(Operation.From(new ShowArtworkOp(_store)));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static JsonElement Painted(string html = "<p>swatches</p>") =>
        JsonDocument.Parse($$"""{ "id": "{{Palette}}", "title": "Amethyst palette", "html": "{{html}}", "painted": "2026-11-07T12:00:00+00:00" }""").RootElement;

    private static async IAsyncEnumerable<string> Signals(params string[] signals)
    {
        foreach (var signal in signals)
        {
            await Task.Yield();
            yield return signal;
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void LendsThePaintingAPaintGaveBack()
    {
        var loan = Assert.IsType<PaintingLoan>(_lender.Lend(Painted()));

        Assert.Equal((Palette, "Amethyst palette", "<p>swatches</p>"), (loan.Id, loan.Title, loan.Html));
        Assert.Equal(($"painting:{Palette}", "Painting"), (loan.Key, loan.Source));
    }

    [Theory]
    [InlineData("""{ "id": "not-a-guid", "title": "A", "html": "<p></p>", "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData("""{ "id": "1d9b883c-b335-4dc4-b228-6bf3aeb6802c", "title": "A", "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData("""{ "name": "Sea", "colors": ["#003F5C"] }""")]
    [InlineData("[1, 2]")]
    public void LendsNothingElse(string said)
    {
        Assert.Null(_lender.Lend(JsonDocument.Parse(said).RootElement));
    }

    [Fact]
    public async Task ReadsThePaintingAgainWhenFirstSeenSinceWhatWasKeptMayBeOlder()
    {
        await PaintAsync("<p>newer</p>");
        var loan = Lent();

        loan.IsLive = true;

        await Until(() => loan.Html == "<p>newer</p>");
    }

    [Fact]
    public async Task RedrawsWhileSeenWhenThePaintingIsRepainted()
    {
        await PaintAsync("<p>first</p>");
        var loan = Lent();
        loan.IsLive = true;
        await Until(() => loan.Html == "<p>first</p>");

        await PaintAsync("<p>second</p>");
        await _lender.FollowAsync(Signals("painting"), Token);

        await Until(() => loan.Html == "<p>second</p>");
    }

    [Fact]
    public async Task LetsRepaintsGoByWhileOutOfSightAndCatchesUpWhenSeenAgain()
    {
        await PaintAsync("<p>first</p>");
        var loan = Lent();
        loan.IsLive = true;
        await Until(() => loan.Html == "<p>first</p>");
        loan.IsLive = false;

        await PaintAsync("<p>second</p>");
        await _lender.FollowAsync(Signals("painting", "pins"), Token);
        await Task.Delay(50, Token);

        Assert.Equal("<p>first</p>", loan.Html);

        loan.IsLive = true;

        await Until(() => loan.Html == "<p>second</p>");
    }

    [Fact]
    public async Task KeepsWhatItHasWhenThePaintingIsGone()
    {
        var loan = Lent();

        await loan.RefreshAsync(Token);

        Assert.Equal("<p>swatches</p>", loan.Html);
    }

    private PaintingLoan Lent() => Assert.IsType<PaintingLoan>(_lender.Lend(Painted()));

    private async Task PaintAsync(string html)
    {
        var listed = await _store.ListAsync(Token);
        var painted = listed.Value.Any(artwork => artwork.Id == Palette)
            ? await _store.ChangeAsync(Palette, artwork => artwork with { Html = html }, Token)
            : await _store.AddAsync(new Artwork(Palette, "Amethyst palette", html, DateTimeOffset.UnixEpoch), Token);

        Assert.True(painted.IsSuccess);
    }

    private static async Task Until(Func<bool> done)
    {
        for (var tries = 0; !done() && tries < 100; tries++)
        {
            await Task.Delay(10, Token);
        }

        Assert.True(done());
    }
}
