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

    private static JsonElement Painted(int version, int versions) =>
        JsonDocument.Parse($$"""{ "id": "{{Palette}}", "title": "Amethyst palette", "version": {{version}}, "versions": {{versions}}, "painted": "2026-11-07T12:00:00+00:00" }""").RootElement;

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
    public void LendsTheVersionAPaintGaveBack()
    {
        var loan = Assert.IsType<PaintingLoan>(_lender.Lend(Painted(2, 2)));

        Assert.Equal((Palette, "Amethyst palette", 2, 2), (loan.Id, loan.Title, loan.Version, loan.Versions));
        Assert.Equal(($"painting:{Palette}:2", "Painting"), (loan.Key, loan.Source));
    }

    [Fact]
    public void LendsWhatAnOlderPaintGaveBackAsTheFirstVersion()
    {
        var said = JsonDocument.Parse($$"""{ "id": "{{Palette}}", "title": "A", "html": "<p>old</p>", "painted": "2026-11-07T12:00:00+00:00" }""").RootElement;

        var loan = Assert.IsType<PaintingLoan>(_lender.Lend(said));

        Assert.Equal((1, "<p>old</p>"), (loan.Version, loan.Html));
    }

    [Theory]
    [InlineData("""{ "id": "not-a-guid", "title": "A", "version": 1, "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData("""{ "id": "1d9b883c-b335-4dc4-b228-6bf3aeb6802c", "version": 1, "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData("""{ "name": "Sea", "colors": ["#003F5C"] }""")]
    [InlineData("[1, 2]")]
    public void LendsNothingElse(string said)
    {
        Assert.Null(_lender.Lend(JsonDocument.Parse(said).RootElement));
    }

    [Fact]
    public async Task ReadsItsOwnVersionWhenFirstSeen()
    {
        await PaintAsync("<p>one</p>", "<p>two</p>");
        var loan = Lent(1, 1);

        loan.IsLive = true;

        await Until(() => loan.Html == "<p>one</p>");
        Assert.Equal((2, false, true), (loan.Versions, loan.IsNewest, loan.IsPainted));
    }

    [Fact]
    public async Task KeepsItsVersionWhenThePaintingIsPaintedAgain()
    {
        await PaintAsync("<p>one</p>");
        var loan = Lent(1, 1);
        loan.IsLive = true;
        await Until(() => loan.Html == "<p>one</p>");

        await PaintAsync("<p>two</p>");
        await _lender.FollowAsync(Signals("painting"), Token);

        await Until(() => loan.Versions == 2);
        Assert.Equal("<p>one</p>", loan.Html);
    }

    [Fact]
    public async Task ShowsTheOtherVersionsWithoutForgettingItsOwn()
    {
        await PaintAsync("<p>one</p>", "<p>two</p>");
        var loan = Lent(1, 2);
        loan.IsLive = true;
        await Until(() => loan.Html == "<p>one</p>");

        await loan.NextCommand.ExecuteAsync(null);

        Assert.Equal(("<p>two</p>", 2, true, false), (loan.Html, loan.Shown, loan.IsNewest, loan.IsPainted));
        Assert.False(loan.NextCommand.CanExecute(null));

        await loan.PreviousCommand.ExecuteAsync(null);

        Assert.Equal(("<p>one</p>", true), (loan.Html, loan.IsPainted));
        Assert.False(loan.PreviousCommand.CanExecute(null));
    }

    [Fact]
    public async Task KeepsWhatItHasWhenThePaintingIsGone()
    {
        var said = JsonDocument.Parse($$"""{ "id": "{{Palette}}", "title": "A", "html": "<p>kept</p>", "painted": "2026-11-07T12:00:00+00:00" }""").RootElement;
        var loan = Assert.IsType<PaintingLoan>(_lender.Lend(said));

        await loan.RefreshAsync(Token);

        Assert.Equal("<p>kept</p>", loan.Html);
    }

    private PaintingLoan Lent(int version, int versions) => Assert.IsType<PaintingLoan>(_lender.Lend(Painted(version, versions)));

    private async Task PaintAsync(params string[] pages)
    {
        foreach (var html in pages)
        {
            var found = await _store.FindAsync(Palette, Token);
            var painted = found.IsSuccess
                ? await _store.ChangeAsync(Palette, artwork => artwork.Painted(html, DateTimeOffset.UnixEpoch), Token)
                : await _store.AddAsync(new Artwork(Palette, "Amethyst palette", null, [new ArtworkVersion(1, html, DateTimeOffset.UnixEpoch)]), Token);

            Assert.True(painted.IsSuccess);
        }
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
