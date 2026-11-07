using Overworld.Bell;

namespace Overworld.Bell.Tests;

public sealed class NoticeBoardTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("bell-").FullName;

    private NoticeBoard Board() => new(Path.Combine(_folder, "notices.json"));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Notice Asking(string id, string from = "crafter") => new(
        id,
        from,
        "Which storage?",
        null,
        null,
        null,
        new Questions([new Question("Storage", "Where?", false, [new Choice("Chest", null), new Choice("Memory", null)])]),
        false,
        DateTimeOffset.UnixEpoch);

    private static Notice Permission(string id) => Asking(id) with
    {
        Ask = new Buttons("just test", [new Button("deny", "Deny", Tone.Danger), new Button("once", "Allow once", Tone.Primary)]),
    };

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task ListsNothingBeforeAnythingRings()
    {
        Assert.Empty((await Board().ListAsync(Token)).Value);
    }

    [Fact]
    public async Task KeepsWhatRangAcrossRestarts()
    {
        await Board().RingAsync(Asking("a"), Token);
        await Board().RingAsync(Permission("b"), Token);

        var notices = (await Board().ListAsync(Token)).Value;

        Assert.Equal(["a", "b"], notices.Select(notice => notice.Id));
        Assert.IsType<Buttons>(notices[1].Ask);
    }

    [Fact]
    public async Task AnAnsweredNoticeStopsWaiting()
    {
        await Board().RingAsync(Asking("a"), Token);

        var answered = await Board().AnswerAsync("a", new Answered([new Picked(["Chest"])]), Token);

        Assert.True(answered.IsSuccess);
        Assert.Empty((await Board().ListAsync(Token)).Value);
    }

    [Fact]
    public async Task AnswersOnlyOnce()
    {
        await Board().RingAsync(Asking("a"), Token);
        await Board().AnswerAsync("a", new Skipped(), Token);

        Assert.Equal("bell.answered", (await Board().AnswerAsync("a", new Skipped(), Token)).Error.Code);
    }

    [Fact]
    public async Task RefusesAReplyThatDoesNotFitTheAsk()
    {
        await Board().RingAsync(Asking("a"), Token);
        await Board().RingAsync(Permission("b"), Token);

        Assert.Equal("bell.reply_unfit", (await Board().AnswerAsync("a", new Answered([]), Token)).Error.Code);
        Assert.Equal("bell.reply_unfit", (await Board().AnswerAsync("a", new Pressed("once"), Token)).Error.Code);
        Assert.Equal("bell.reply_unfit", (await Board().AnswerAsync("b", new Pressed("always"), Token)).Error.Code);
        Assert.Equal("bell.reply_unfit", (await Board().AnswerAsync("b", new Skipped(), Token)).Error.Code);
        Assert.True((await Board().AnswerAsync("b", new Pressed("once"), Token)).IsSuccess);
    }

    [Fact]
    public async Task CannotAnswerWhatNeverRang()
    {
        Assert.Equal("bell.unknown", (await Board().AnswerAsync("nope", new Skipped(), Token)).Error.Code);
    }

    [Fact]
    public async Task WithdrawingTakesTheNoticeAway()
    {
        await Board().RingAsync(Asking("a"), Token);

        Assert.Equal("a", (await Board().WithdrawAsync("a", Token)).Value.Id);
        Assert.Empty((await Board().ListAsync(Token)).Value);
        Assert.Equal("bell.unknown", (await Board().WithdrawAsync("a", Token)).Error.Code);
    }

    [Fact]
    public async Task CollectingHandsTheAppItsAnswersOnce()
    {
        await Board().RingAsync(Asking("a"), Token);
        await Board().RingAsync(Asking("b"), Token);
        await Board().AnswerAsync("a", new Answered([new Picked([], "Somewhere else")]), Token);

        var collected = (await Board().CollectAsync("crafter", Token)).Value;

        var reply = Assert.IsType<Answered>(Assert.Single(collected).Reply);
        Assert.Equal("Somewhere else", reply.Given[0].Written);
        Assert.Empty((await Board().CollectAsync("crafter", Token)).Value);
        Assert.Equal(["b"], (await Board().ListAsync(Token)).Value.Select(notice => notice.Id));
    }

    [Fact]
    public async Task CollectingHandsAnOpenedNoticeOnceAndKeepsItWaiting()
    {
        await Board().RingAsync(Asking("a"), Token);
        await Board().OpenAsync("a", Token);

        var collected = (await Board().CollectAsync("crafter", Token)).Value;

        Assert.True(Assert.Single(collected).Opened);
        Assert.Empty((await Board().CollectAsync("crafter", Token)).Value);
        Assert.False(Assert.Single((await Board().ListAsync(Token)).Value).Opened);
    }

    [Fact]
    public async Task CollectsOnlyWhatTheAppRang()
    {
        await Board().RingAsync(Asking("a", from: "dye"), Token);
        await Board().AnswerAsync("a", new Skipped(), Token);

        Assert.Empty((await Board().CollectAsync("crafter", Token)).Value);
        Assert.Single((await Board().CollectAsync("dye", Token)).Value);
    }
}
