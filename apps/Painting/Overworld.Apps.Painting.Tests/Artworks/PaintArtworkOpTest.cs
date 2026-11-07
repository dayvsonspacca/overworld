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

        Assert.Equal("Design", painted.Value.Title);
        Assert.Equal("<h1>Hi</h1>", painted.Value.Html);
        Assert.Equal(_clock.Now, painted.Value.Painted);
        Assert.Equal(painted.Value, Assert.Single(await Stored()));
    }

    [Fact]
    public async Task ReadsThePageFromAFile()
    {
        var file = Path.Combine(_folder.Path, "page.html");
        await File.WriteAllTextAsync(file, "<p>From a file</p>", Token);

        var painted = await Paint(new PaintArtwork("Design", File: file));

        Assert.Equal("<p>From a file</p>", painted.Value.Html);
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
    public async Task ChangesAPaintingByItsIdAndKeepsItsTitle()
    {
        var first = await Paint(new PaintArtwork("Design", Html: "<p>one</p>"));
        _clock.Now = _clock.Now.AddMinutes(5);

        var changed = await Paint(new PaintArtwork(Html: "<p>two</p>", Id: first.Value.Id));

        Assert.Equal(new Artwork(first.Value.Id, "Design", "<p>two</p>", _clock.Now), changed.Value);
        Assert.Equal(changed.Value, Assert.Single(await Stored()));
    }

    [Fact]
    public async Task RefusesToChangeAPaintingThatIsNotThere()
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

    private Task<Result<Artwork>> Paint(PaintArtwork input) => _paint.ExecuteAsync(input, Token);

    private async Task<IReadOnlyList<Artwork>> Stored() => (await _store.ListAsync(Token)).Value;

    private static void AssertFails(string code, Result<Artwork> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error.Code);
    }
}
