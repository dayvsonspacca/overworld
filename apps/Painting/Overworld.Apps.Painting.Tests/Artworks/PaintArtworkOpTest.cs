using Overworld.Apps.Painting.Artworks;
using Overworld.Beacon;
using Overworld.Bedrock.Results;
using Overworld.Redstone;

namespace Overworld.Apps.Painting.Tests.Artworks;

public sealed class PaintArtworkOpTest : IDisposable
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

    private readonly PaintArtworkOp _paint;

    public PaintArtworkOpTest()
    {
        _store = new ArtworkStore(_folder.Path);
        _paint = new PaintArtworkOp(_store, new BeaconClient(_daemon), _clock);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task PaintsANewPageAndKeepsIt()
    {
        var painted = await Paint(new PaintArtwork(" Design ", Html: "<h1>Hi</h1>"));

        Assert.Equal(("Design", 1, 1, _clock.Now), (painted.Value.Title, painted.Value.Version, painted.Value.Versions, painted.Value.Painted));
        Assert.Null(painted.Value.Html);
        var kept = Assert.Single(await Stored());
        Assert.Equal((painted.Value.Id, "<h1>Hi</h1>"), (kept.Id, kept.Latest.Html));
    }

    [Fact]
    public async Task ReadsThePageFromAFile()
    {
        var file = Path.Combine(_folder.Path, "page.html");
        await File.WriteAllTextAsync(file, "<p>From a file</p>", Token);

        await Paint(new PaintArtwork("Design", File: file));

        Assert.Equal("<p>From a file</p>", Assert.Single(await Stored()).Latest.Html);
    }

    [Fact]
    public async Task SignalsTheWindowWhenItPaints()
    {
        await Paint(new PaintArtwork("Design", Html: "<p></p>"));

        var request = Assert.Single(_daemon.Requests);
        Assert.Equal("beacon-light", request.Operation);
        Assert.Equal("painting", request.Input!["signal"]!.GetValue<string>());
    }

    [Fact]
    public async Task PaintsAPaintingAgainAsItsNextVersionAndKeepsTheEarlierOnes()
    {
        var first = await Paint(new PaintArtwork("Design", Html: "<p>one</p>"));
        _clock.Now = _clock.Now.AddMinutes(5);

        var again = await Paint(new PaintArtwork(Html: "<p>two</p>", Id: first.Value.Id));

        Assert.Equal((first.Value.Id, "Design", 2, 2, _clock.Now), (again.Value.Id, again.Value.Title, again.Value.Version, again.Value.Versions, again.Value.Painted));
        var kept = Assert.Single(await Stored());
        Assert.Equal(["<p>one</p>", "<p>two</p>"], kept.Versions.Select(version => version.Html));
        Assert.Equal([1, 2], kept.Versions.Select(version => version.Number));
    }

    [Fact]
    public async Task GivesAGroupAndKeepsItWhenPaintedAgain()
    {
        var first = await Paint(new PaintArtwork("Buttons", Html: "<p>one</p>", Group: ArtworkGroup.Design));

        var again = await Paint(new PaintArtwork(Html: "<p>two</p>", Id: first.Value.Id));

        Assert.Equal(ArtworkGroup.Design, again.Value.Group);
        Assert.Equal(ArtworkGroup.Design, Assert.Single(await Stored()).Group);
    }

    [Fact]
    public async Task RefusesToPaintAgainAPaintingThatIsNotThere()
    {
        AssertFails("painting.not_found", await Paint(new PaintArtwork(Html: "<p></p>", Id: Guid.NewGuid())));
        Assert.Empty(_daemon.Requests);
    }

    [Fact]
    public async Task RefusesANewPaintingWithoutATitle()
    {
        AssertFails("painting.title_missing", await Paint(new PaintArtwork(Html: "<p></p>")));
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task RefusesABlankTitle()
    {
        AssertFails("painting.title_empty", await Paint(new PaintArtwork("  ", Html: "<p></p>")));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("<p></p>", "page.html")]
    public async Task RefusesAnythingButOneOfHtmlAndFile(string? html, string? file)
    {
        AssertFails("painting.html_or_file", await Paint(new PaintArtwork("Design", html, file)));
    }

    [Fact]
    public async Task RefusesAFileThatIsNotThere()
    {
        AssertFails("painting.file_not_found", await Paint(new PaintArtwork("Design", File: Path.Combine(_folder.Path, "gone.html"))));
    }

    private Task<Result<Canvas>> Paint(PaintArtwork input) => _paint.ExecuteAsync(input, Token);

    private async Task<IReadOnlyList<Artwork>> Stored() => (await _store.ListAsync(Token)).Value;

    private static void AssertFails(string code, Result<Canvas> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error.Code);
    }
}
