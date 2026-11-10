using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Remote;
using Overworld.Allay.Settings;
using Overworld.Allay.Tests.Fakes;

namespace Overworld.Allay.Tests.Remote;

public sealed class AllayHostTest : IAsyncDisposable
{
    private static readonly Brief Brief = new("/tmp/project", null, ["Be brief"], [], [new ToolServer("crafter", new Uri("http://127.0.0.1:5555/mcp"), "token", ["list-todos"], TimeSpan.FromMinutes(30))]);

    private static readonly Prompt Hello = new("Hello", [], []);

    private readonly string _folder = Directory.CreateTempSubdirectory("allay-").FullName;

    private readonly string _socket;

    private readonly HeldAgent _agent = new("held");

    private CancellationTokenSource _stop = new();

    private AllayHost _host;

    private Task _running;

    public AllayHostTest()
    {
        _socket = Path.Combine(_folder, "allay.sock");
        (_host, _running) = Serve();
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await _running;
        _host.Dispose();
        _stop.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task AnswersAPromptThroughTheSocket()
    {
        using var link = new AllayLink(_socket);
        var opened = (await link.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token)).Value;

        var replying = Collect(opened.Session.ReplyAsync(Hello, Setup.Default, Token));
        await Until(() => _agent.Sessions.Count == 1 && _agent.Sessions[0].Heard.Count == 1);
        Feed(new Speech("Hi"), new Finished());

        Assert.Equal([new Speech("Hi"), new Finished()], await replying);
        Assert.Equal("Hello", Assert.Single(_agent.Sessions[0].Heard).Text);
    }

    [Fact]
    public async Task KeepsReplyingWhenTheAppLeavesAndHandsBackWhatItMissed()
    {
        using (var first = new AllayLink(_socket))
        {
            var opened = (await first.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token)).Value;
            var replying = opened.Session.ReplyAsync(Hello, Setup.Default, Token).GetAsyncEnumerator(Token);
            var heard = replying.MoveNextAsync();
            await Until(() => _agent.Sessions.Count == 1 && _agent.Sessions[0].Heard.Count == 1);
            Feed(new Speech("Working on it. "));
            Assert.True(await heard);
            opened.Session.Leave();
        }

        Feed(new Speech("Done."), new Finished());
        await Until(() => !_agent.Sessions[0].Feed.Reader.TryPeek(out _));
        await Task.Delay(100, Token);

        using var second = new AllayLink(_socket);
        var back = (await second.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token)).Value;

        Assert.Single(_agent.Sessions);
        Assert.False(_agent.Sessions[0].Ended);
        var reply = Assert.IsType<Reply>(back.Turns[^1]);
        Assert.Equal(new Speech("Working on it. Done."), Assert.Single(reply.Moments));
        Assert.IsType<Finished>(reply.Outcome);
        Assert.Equal(Hello, back.Turns[0]);
    }

    [Fact]
    public async Task HandsBackAReplyStillUnderWayAsItWakesThenGoesOnLive()
    {
        using (var first = new AllayLink(_socket))
        {
            var opened = (await first.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token)).Value;
            var replying = opened.Session.ReplyAsync(Hello, Setup.Default, Token).GetAsyncEnumerator(Token);
            var heard = replying.MoveNextAsync();
            await Until(() => _agent.Sessions.Count == 1 && _agent.Sessions[0].Heard.Count == 1);
            Feed(new Speech("Part one. "));
            Assert.True(await heard);
            opened.Session.Leave();
        }

        using var second = new AllayLink(_socket);
        var back = (await second.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token)).Value;
        var conversation = new Conversation(back.Session, back.Turns);
        var woke = new TaskCompletionSource<IAsyncEnumerable<Moment>>();
        conversation.Woke += (_, moments) => woke.TrySetResult(moments);

        var resumed = Collect(await woke.Task.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Feed(new Speech("Part two."), new Finished());

        Assert.Equal([new Speech("Part one. "), new Speech("Part two."), new Finished()], await resumed);
        Assert.Equal(Hello, Assert.Single(back.Turns));
    }

    [Fact]
    public async Task AdoptsTheHistoryItIsGivenTheFirstTimeAndKeepsItWhenRestarted()
    {
        IReadOnlyList<Turn> history = [new Prompt("Before", [], []), new Reply([new Speech("Earlier")], new Finished())];

        using (var first = new AllayLink(_socket))
        {
            var opened = (await first.OpenAsync("chat-1", "held", Brief, "session-7", history, keep: true, Token)).Value;
            Assert.Equal(history, opened.Turns);
        }

        Assert.Equal("session-7", _agent.Sessions[0].Resumed);
        await RestartAsync();

        using var second = new AllayLink(_socket);
        var back = (await second.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token)).Value;

        Assert.Equal(history, back.Turns);
        Assert.True(_agent.Sessions[0].Ended);
        Assert.Equal("held", _agent.Sessions[1].Resumed);
    }

    [Fact]
    public async Task TellsTheSessionWhatTheAppDecidesAndRebriefsWhenOpenedAgain()
    {
        using var link = new AllayLink(_socket);
        var opened = (await link.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token)).Value;

        opened.Session.Decide("ask-1", new AllowOnce());
        var newer = Brief with { Context = "Newer" };
        await link.OpenAsync("chat-1", "held", newer, null, [], keep: true, Token);

        await Until(() => _agent.Sessions[0].Decided.Count == 1);
        Assert.Equal("ask-1", _agent.Sessions[0].Decided[0].Request);
        Assert.Equal("Newer", _agent.Sessions[0].Brief.Context);
        Assert.Equal("output of job-1", opened.Session.Output("job-1"));
    }

    [Fact]
    public async Task EndsADisposableChatOnceItsAppLeaves()
    {
        using (var link = new AllayLink(_socket))
        {
            var opened = (await link.OpenAsync("theme-chat", "held", Brief, null, [], keep: false, Token)).Value;
            opened.Session.Leave();
        }

        await Until(() => _agent.Sessions[0].Ended);
        Assert.False(Directory.Exists(Path.Combine(_folder, "chats")) && Directory.EnumerateFiles(Path.Combine(_folder, "chats")).Any());
    }

    [Fact]
    public async Task RefusesAnAgentItDoesNotKnow()
    {
        using var link = new AllayLink(_socket);

        var opened = await link.OpenAsync("chat-1", "nobody", Brief, null, [], keep: true, Token);

        Assert.Equal("allay.refused", opened.Error.Code);
    }

    [Fact]
    public void SaysWhenNothingIsServing()
    {
        Assert.False(AllayLink.IsServing(Path.Combine(_folder, "nothing.sock")));
        Assert.True(AllayLink.IsServing(_socket));
    }

    private (AllayHost, Task) Serve()
    {
        var host = new AllayHost(new Crew([_agent], new StaffFile(_folder)), new Transcripts(Path.Combine(_folder, "chats")), NullLogger<AllayHost>.Instance, _socket);
        host.Start();

        return (host, host.RunAsync(_stop.Token));
    }

    private async Task RestartAsync()
    {
        await _stop.CancelAsync();
        await _running;
        _host.Dispose();
        _stop.Dispose();
        _stop = new CancellationTokenSource();
        (_host, _running) = Serve();
    }

    private void Feed(params Moment[] moments)
    {
        foreach (var moment in moments)
        {
            _agent.Sessions[^1].Feed.Writer.TryWrite(moment);
        }
    }

    private static async Task<List<Moment>> Collect(IAsyncEnumerable<Moment> moments)
    {
        List<Moment> heard = [];

        await foreach (var moment in moments.WithCancellation(Token))
        {
            heard.Add(moment);
        }

        return heard;
    }

    private static async Task Until(Func<bool> done)
    {
        for (var tries = 0; !done() && tries < 200; tries++)
        {
            await Task.Delay(10, Token);
        }

        Assert.True(done());
    }
}
