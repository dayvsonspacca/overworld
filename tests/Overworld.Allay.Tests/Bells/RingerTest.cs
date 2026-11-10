using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Allay.Conversations;
using Overworld.Allay.Bells;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bell;
using Overworld.Redstone;

namespace Overworld.Allay.Tests.Bells;

public sealed class RingerTest : IAsyncLifetime
{
    private static readonly Permission Run = new("p", ActKind.Command, "just test", null, []);

    private readonly string _folder = Directory.CreateTempSubdirectory("ringer-").FullName;
    private readonly CancellationTokenSource _stop = new();
    private NoticeBoard _board = null!;
    private RedstoneServer _server = null!;
    private Task _serving = Task.CompletedTask;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
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

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await _serving;
        _server.Dispose();
        _stop.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    private Ringer Ringer() => new(new BellClient(new RedstoneClient(Path.Combine(_folder, "overworld.sock"))), "crafter");

    [Fact]
    public async Task HandsTheAnswerBackAsADecision()
    {
        var ringer = Ringer();
        Decision? decided = null;
        var notice = await ringer.RingAsync(Run, new Ringing("overworld", null, false, decision => decided = decision, () => { }), Token);
        var once = Assert.IsType<Buttons>(Assert.Single((await _board.ListAsync(Token)).Value).Ask).Offered[^1].Key;
        await _board.AnswerAsync(notice!, new Pressed(once), Token);

        await ringer.FollowAsync(AsyncEnumerable.Empty<string>(), Token);

        Assert.IsType<AllowOnce>(decided);
    }

    [Fact]
    public async Task AsksToOpenWhenTheNoticeIsOpened()
    {
        var ringer = Ringer();
        var opened = 0;
        var notice = await ringer.RingAsync(Run, new Ringing("overworld", null, false, _ => { }, () => opened++), Token);
        await _board.OpenAsync(notice!, Token);

        await ringer.FollowAsync(AsyncEnumerable.Empty<string>(), Token);

        Assert.Equal(1, opened);
        Assert.Single((await _board.ListAsync(Token)).Value);
    }

    [Fact]
    public async Task WithdrawsEverythingItRang()
    {
        var ringer = Ringer();
        var ringing = new Ringing("overworld", null, false, _ => { }, () => { });
        await ringer.RingAsync(Run, ringing, Token);
        await ringer.RingAsync(Run, ringing, Token);

        await ringer.WithdrawAllAsync(Token);

        Assert.Empty((await _board.ListAsync(Token)).Value);
    }

    [Fact]
    public async Task RingsNothingWhenTheDaemonIsAway()
    {
        var away = new Ringer(new BellClient(new RedstoneClient(Path.Combine(_folder, "nobody.sock"))), "crafter");

        Assert.Null(await away.RingAsync(Run, new Ringing("overworld", null, false, _ => { }, () => { }), Token));
    }
}
