using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Allay.Bells;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Presentation.Chats;
using Overworld.Allay.Tests.Fakes;
using Overworld.Allay.Visits;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bell;
using Overworld.Redstone;

namespace Overworld.Allay.Tests.Chats;

public sealed class ChatBellTest : IAsyncLifetime
{
    private static readonly Brief Brief = new("/tmp/dye", "You are inside Dye.", [], [], []);

    private readonly string _folder = Directory.CreateTempSubdirectory("chat-bell-").FullName;
    private readonly CancellationTokenSource _stop = new();
    private readonly HeldAgent _agent = new("held");
    private NoticeBoard _board = null!;
    private RedstoneServer _server = null!;
    private Task _serving = Task.CompletedTask;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _board = new NoticeBoard(Path.Combine(_folder, "notices.json"));
        var beam = new Beam();
        _server = new RedstoneServer(
            Path.Combine(_folder, "overworld.sock"),
            new Dispatcher(
                [
                    Operation.From(new RingBellOp(_board, beam, TimeProvider.System)),
                    Operation.From(new WithdrawNoticeOp(_board, beam)),
                    Operation.From(new CollectNoticesOp(_board)),
                ],
                NullLogger<Dispatcher>.Instance),
            NullLogger<RedstoneServer>.Instance);
        _server.Start();
        _serving = _server.RunAsync(_stop.Token);
        await Crew().HireAsync("held", true, Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await _serving;
        _server.Dispose();
        _stop.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task RingsWhatTheAgentAsksAndWithdrawsItOnceAnsweredInTheChat()
    {
        var chat = await AskingAsync();

        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 1);
        var notice = Assert.Single((await _board.ListAsync(Token)).Value);
        Assert.Equal("Dye", notice.Context);

        chat.Timeline.Request("p")!.Settle(new AllowOnce());

        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 0);
        Assert.Equal(("p", (Decision)new AllowOnce()), Assert.Single(_agent.Sessions[0].Decided));
    }

    [Fact]
    public async Task WithdrawsWhatItRangWhenTheAppIsDone()
    {
        var chat = await AskingAsync();
        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 1);

        chat.End();

        await Until(async () => (await _board.ListAsync(Token)).Value.Count == 0);
    }

    private Crew Crew() => new([_agent], new StaffFile(_folder));

    private async Task<ChatViewModel> AskingAsync()
    {
        var ringer = new Ringer(new BellClient(new RedstoneClient(Path.Combine(_folder, "overworld.sock"))), "dye");
        var chat = new ChatViewModel(new Lobby(Crew()), _ => Task.FromResult(Brief), ringer: ringer, context: "Dye");
        await chat.LoadAsync(Token);
        chat.Draft = "Save it";
        _ = chat.SendCommand.ExecuteAsync(null);
        await Until(() => Task.FromResult(_agent.Sessions is [{ Heard.Count: 1 }]));
        _agent.Sessions[0].Feed.Writer.TryWrite(new Permission("p", ActKind.Tool, "dye: themes save", null, []));
        await Until(() => Task.FromResult(chat.Timeline.Request("p") is not null));

        return chat;
    }

    private static async Task Until(Func<Task<bool>> condition)
    {
        for (var tries = 0; !await condition() && tries < 300; tries++)
        {
            await Task.Delay(10, Token);
        }

        Assert.True(await condition());
    }
}
