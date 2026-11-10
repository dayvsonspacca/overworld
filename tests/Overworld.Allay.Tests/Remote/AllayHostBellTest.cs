using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Allay.Bells;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Remote;
using Overworld.Allay.Settings;
using Overworld.Allay.Tests.Fakes;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bell;
using Overworld.Redstone;

namespace Overworld.Allay.Tests.Remote;

public sealed class AllayHostBellTest : IAsyncLifetime
{
    private static readonly Brief Brief = new("/home/me/overworld", null, [], [], []);

    private static readonly Permission Run = new("ask-1", ActKind.Command, "just test", null, []);

    private readonly string _folder = Directory.CreateTempSubdirectory("allay-bell-").FullName;

    private readonly HeldAgent _agent = new("held");

    private readonly CancellationTokenSource _stop = new();

    private NoticeBoard _board = null!;

    private RedstoneServer _daemon = null!;

    private Ringer _ringer = null!;

    private AllayHost _host = null!;

    private Task _running = Task.CompletedTask;

    private readonly List<IReadOnlyList<string>> _launched = [];

    private string Socket => Path.Combine(_folder, "allay.sock");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        _board = new NoticeBoard(Path.Combine(_folder, "notices.json"));
        var beam = new Beam();
        _daemon = new RedstoneServer(
            Path.Combine(_folder, "overworld.sock"),
            new Dispatcher(
                [
                    Operation.From(new RingBellOp(_board, beam, TimeProvider.System)),
                    Operation.From(new WithdrawNoticeOp(_board, beam)),
                    Operation.From(new CollectNoticesOp(_board)),
                ],
                NullLogger<Dispatcher>.Instance),
            NullLogger<RedstoneServer>.Instance);
        _daemon.Start();
        _ringer = new Ringer(new BellClient(new RedstoneClient(Path.Combine(_folder, "overworld.sock"))), "allay");
        _host = new AllayHost(new Crew([_agent], new StaffFile(_folder)), new Transcripts(Path.Combine(_folder, "chats")), NullLogger<AllayHost>.Instance, Socket, _ringer, _launched.Add);
        _host.Start();
        _running = Task.WhenAll(_daemon.RunAsync(_stop.Token), _host.RunAsync(_stop.Token));

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await _running;
        _host.Dispose();
        _daemon.Dispose();
        _stop.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task RingsTheBellForARequestNobodyIsWatchingAndDecidesWithTheAnswer()
    {
        await LeaveWhileReplyingAsync();

        _agent.Sessions[0].Feed.Writer.TryWrite(Run);
        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 1);

        var notice = Assert.Single((await _board.ListAsync(Token)).Value);
        Assert.Equal(("allay", "overworld"), (notice.From, notice.Context));
        var once = Assert.IsType<Buttons>(notice.Ask).Offered[^1].Key;
        await _board.AnswerAsync(notice.Id, new Pressed(once), Token);
        await _ringer.FollowAsync(AsyncEnumerable.Empty<string>(), Token);

        await Until(() => Task.FromResult(_agent.Sessions[0].Decided.Count == 1));
        Assert.Equal("ask-1", _agent.Sessions[0].Decided[0].Request);
        Assert.IsType<AllowOnce>(_agent.Sessions[0].Decided[0].Decision);
    }

    [Fact]
    public async Task TakesTheNoticeBackOnceAnAppWatchesTheChatAgain()
    {
        await LeaveWhileReplyingAsync();
        _agent.Sessions[0].Feed.Writer.TryWrite(Run);
        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 1);

        using var back = new AllayLink(Socket);
        await back.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token);

        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 0);
    }

    [Fact]
    public async Task BringsTheChatBackWithItsOpenerWhenTheNoticeIsOpened()
    {
        await LeaveWhileReplyingAsync(["crafter", "summon", "/home/me/overworld", "chat-1"]);
        _agent.Sessions[0].Feed.Writer.TryWrite(Run);
        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 1);

        await _board.OpenAsync(Assert.Single((await _board.ListAsync(Token)).Value).Id, Token);
        await _ringer.FollowAsync(AsyncEnumerable.Empty<string>(), Token);

        Assert.Equal(["crafter", "summon", "/home/me/overworld", "chat-1"], Assert.Single(_launched));
    }

    private async Task LeaveWhileReplyingAsync(IReadOnlyList<string>? opener = null)
    {
        using var link = new AllayLink(Socket);
        var opened = (await link.OpenAsync("chat-1", "held", Brief, null, [], keep: true, Token, opener)).Value;
        var replying = opened.Session.ReplyAsync(new Prompt("Run the tests", [], []), Setup.Default, Token).GetAsyncEnumerator(Token);
        var heard = replying.MoveNextAsync();
        await Until(() => Task.FromResult(_agent.Sessions.Count == 1 && _agent.Sessions[0].Heard.Count == 1));
        _agent.Sessions[0].Feed.Writer.TryWrite(new Speech("Running them. "));
        Assert.True(await heard);
        opened.Session.Leave();
        await Task.Delay(100, Token);
    }

    private static async Task Until(Func<Task<bool>> done)
    {
        for (var tries = 0; !await done() && tries < 200; tries++)
        {
            await Task.Delay(10, Token);
        }

        Assert.True(await done());
    }
}
