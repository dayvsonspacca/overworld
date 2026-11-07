using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Redstone;

namespace Overworld.Bell.Tests;

public sealed class BellClientTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("bell-").FullName;

    private string Socket => Path.Combine(_folder, "overworld.sock");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task RingsWithdrawsAndCollectsThroughTheDaemon()
    {
        var board = new NoticeBoard(Path.Combine(_folder, "notices.json"));
        var beam = new Beam();
        using var server = new RedstoneServer(
            Socket,
            new Dispatcher(
                [
                    Operation.From(new RingBellOp(board, beam, TimeProvider.System)),
                    Operation.From(new WithdrawNoticeOp(board, beam)),
                    Operation.From(new CollectNoticesOp(board)),
                ],
                NullLogger<Dispatcher>.Instance),
            NullLogger<RedstoneServer>.Instance);
        server.Start();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var serving = server.RunAsync(stop.Token);
        var bell = new BellClient(new RedstoneClient(Socket));

        var kept = await bell.RingAsync(new RingBell("crafter", "Kept", Ask: new Buttons(null, [new Button("ok", "OK")])), Token);
        var gone = await bell.RingAsync(new RingBell("crafter", "Gone"), Token);
        await board.AnswerAsync(kept.Value.Id, new Pressed("ok"), Token);

        Assert.Equal("Gone", (await bell.WithdrawAsync(gone.Value.Id, Token)).Value.Title);
        Assert.Equal(new Pressed("ok"), Assert.Single((await bell.CollectAsync("crafter", Token)).Value).Reply);
        Assert.Equal("bell.unknown", (await bell.WithdrawAsync(gone.Value.Id, Token)).Error.Code);

        await stop.CancelAsync();
        await serving;
    }
}
