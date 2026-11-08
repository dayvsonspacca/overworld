using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Apps.Painting.Artworks;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class PaintingsTest : IDisposable
{
    private static readonly Guid Palette = Guid.Parse("1d9b883c-b335-4dc4-b228-6bf3aeb6802c");

    private readonly Bench _bench = new();

    private static string Painted(string html = "<p>swatches</p>") =>
        $$"""{ "id": "{{Palette}}", "title": "Amethyst palette", "html": "{{html}}", "painted": "2026-11-07T12:00:00+00:00" }""";

    private static async IAsyncEnumerable<string> Signals(params string[] signals)
    {
        foreach (var signal in signals)
        {
            await Task.Yield();
            yield return signal;
        }
    }

    [Theory]
    [InlineData(ActKind.Tool)]
    [InlineData(ActKind.Command)]
    public void FramesThePaintingAnActPaintedRightUnderIt(ActKind kind)
    {
        var timeline = new Timeline(gallery: _bench.Gallery);
        timeline.Add(new Act("1", kind, "overworld:painting paint"));
        timeline.Add(new Speech("Here it is."));

        timeline.Add(new ActResult("1", true, Painted()));

        var framed = Assert.IsType<PaintingEntry>(timeline.Entries[1]);
        Assert.Equal((Palette, "Amethyst palette", "<p>swatches</p>"), (framed.Id, framed.Title, framed.Html));
        Assert.IsType<SpeechEntry>(timeline.Entries[2]);
    }

    [Fact]
    public void FramesAPaintingOnceThoughItIsPaintedAgain()
    {
        var timeline = new Timeline(gallery: _bench.Gallery);
        timeline.Add(new Act("1", ActKind.Tool, "crafter: paint"));
        timeline.Add(new ActResult("1", true, Painted()));
        timeline.Add(new Act("2", ActKind.Tool, "crafter: paint"));

        timeline.Add(new ActResult("2", true, Painted("<p>repainted</p>")));

        Assert.Single(timeline.Entries.OfType<PaintingEntry>());
        Assert.IsType<ActEntry>(timeline.Entries[^1]);
    }

    [Theory]
    [InlineData(false, """{ "id": "1d9b883c-b335-4dc4-b228-6bf3aeb6802c", "title": "A", "html": "<p></p>", "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData(true, """{ "id": "not-a-guid", "title": "A", "html": "<p></p>", "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData(true, """{ "id": "1d9b883c-b335-4dc4-b228-6bf3aeb6802c", "title": "A", "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData(true, """{ "id": 3, "title": "Buy milk", "done": false }""")]
    [InlineData(true, "[1, 2]")]
    public void FramesNothingForAnyOtherResult(bool succeeded, string output)
    {
        var timeline = new Timeline(gallery: _bench.Gallery);
        timeline.Add(new Act("1", ActKind.Tool, "something"));

        timeline.Add(new ActResult("1", succeeded, output));

        Assert.IsType<ActEntry>(Assert.Single(timeline.Entries));
    }

    [Fact]
    public void FramesNothingWithoutAGallery()
    {
        var timeline = new Timeline();
        timeline.Add(new Act("1", ActKind.Tool, "crafter: paint"));

        timeline.Add(new ActResult("1", true, Painted()));

        Assert.IsType<ActEntry>(Assert.Single(timeline.Entries));
    }

    [Fact]
    public async Task ReadsThePaintingAgainWhenFirstSeenSinceTheChatMayKeepAnOlderOne()
    {
        await PaintAsync("<p>newer</p>");
        var framed = Framed();

        framed.IsLive = true;

        await Until(() => framed.Html == "<p>newer</p>");
    }

    [Fact]
    public async Task RedrawsWhileSeenWhenThePaintingIsRepainted()
    {
        await PaintAsync("<p>first</p>");
        var framed = Framed();
        framed.IsLive = true;
        await Until(() => framed.Html == "<p>first</p>");

        await PaintAsync("<p>second</p>");
        await _bench.Gallery.FollowAsync(Signals("painting"), Bench.Cancellation);

        await Until(() => framed.Html == "<p>second</p>");
    }

    [Fact]
    public async Task LetsRepaintsGoByWhileOutOfSightAndCatchesUpWhenSeenAgain()
    {
        await PaintAsync("<p>first</p>");
        var framed = Framed();
        framed.IsLive = true;
        await Until(() => framed.Html == "<p>first</p>");
        framed.IsLive = false;

        await PaintAsync("<p>second</p>");
        await _bench.Gallery.FollowAsync(Signals("painting", "pins"), Bench.Cancellation);
        await Task.Delay(50, Bench.Cancellation);

        Assert.Equal("<p>first</p>", framed.Html);

        framed.IsLive = true;

        await Until(() => framed.Html == "<p>second</p>");
    }

    [Fact]
    public async Task KeepsWhatItHasWhenThePaintingIsGone()
    {
        var framed = Framed();

        await framed.RefreshAsync(Bench.Cancellation);

        Assert.Equal("<p>swatches</p>", framed.Html);
    }

    [Fact]
    public async Task SpreadsAPaintingOverTheConversationUntilClosed()
    {
        var folder = await _bench.ProjectAsync("overworld");
        var conversation = new ConversationViewModel(folder, Bench.Chat("Palettes"), _bench.Abilities);
        var framed = Framed();

        conversation.ExpandPaintingCommand.Execute(framed);

        Assert.Same(framed, conversation.Spread);

        conversation.CloseSpreadCommand.Execute(null);

        Assert.Null(conversation.Spread);
    }

    public void Dispose() => _bench.Dispose();

    private PaintingEntry Framed()
    {
        var timeline = new Timeline(gallery: _bench.Gallery);
        timeline.Add(new Act("1", ActKind.Tool, "crafter: paint"));
        timeline.Add(new ActResult("1", true, Painted()));

        return Assert.IsType<PaintingEntry>(timeline.Entries[1]);
    }

    private async Task PaintAsync(string html)
    {
        var listed = await _bench.Artworks.ListAsync(Bench.Cancellation);
        var painted = listed.Value.Any(artwork => artwork.Id == Palette)
            ? await _bench.Artworks.ChangeAsync(Palette, artwork => artwork with { Html = html }, Bench.Cancellation)
            : await _bench.Artworks.AddAsync(new Artwork(Palette, "Amethyst palette", html, DateTimeOffset.UnixEpoch), Bench.Cancellation);

        Assert.True(painted.IsSuccess);
    }

    private static async Task Until(Func<bool> done)
    {
        for (var tries = 0; !done() && tries < 100; tries++)
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        Assert.True(done());
    }
}
