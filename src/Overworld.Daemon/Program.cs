using Microsoft.Extensions.Logging;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bell;
using Overworld.Chest;
using Overworld.Daemon.Lifetime;
using Overworld.Daemon.Logging;
using Overworld.Daemon.Pins;
using Overworld.Daemon.Shell;
using Overworld.Daemon.Spawner;
using Overworld.Hosting;
using Overworld.Redstone;

namespace Overworld.Daemon;

internal static class Program
{
    private const string Command = "overworld";

    private static Task<int> Main(string[] args)
    {
        var client = new RedstoneClient(SocketPath.Resolve());

        return AppHost.RunAsync(
            Command,
            [.. Operations(new Beam()).Select(operation => new RemoteOperation(operation, client))],
            args,
            withoutArgs: RunDaemonAsync);
    }

    private static Operation[] Operations(Beam beam)
    {
        var pins = DataDirectory.For("pins");
        var places = new PinPlaces(new Dictionary<string, PinStore>
        {
            ["menu"] = new(Path.Combine(pins, "menu.json")),
            ["taskbar"] = new(Path.Combine(pins, "taskbar.json")),
        });
        var board = new NoticeBoard(Path.Combine(DataDirectory.For("bell"), "notices.json"));

        return
        [
            Operation.From(new PingOp()),
            Operation.From(new LightBeaconOp(beam)),
            Operation.From(new ListPinsOp(places)),
            Operation.From(new AddPinOp(places, beam)),
            Operation.From(new RemovePinOp(places, beam)),
            Operation.From(new RingBellOp(board, beam, TimeProvider.System)),
            Operation.From(new ListNoticesOp(board)),
            Operation.From(new AnswerNoticeOp(board, beam)),
            Operation.From(new OpenNoticeOp(board, beam)),
            Operation.From(new WithdrawNoticeOp(board, beam)),
            Operation.From(new CollectNoticesOp(board)),
        ];
    }

    private static async Task<int> RunDaemonAsync()
    {
        using var logging = LoggerFactory.Create(builder => builder.AddOverworldConsole());
        using var shutdown = new ShutdownSignal();
        var beam = new Beam();

        using var server = new RedstoneServer(
            SocketPath.Resolve(),
            new Dispatcher(Operations(beam), logging.CreateLogger<Dispatcher>()),
            logging.CreateLogger<RedstoneServer>());

        using var beacon = new BeaconServer(
            SocketPath.Resolve(BeaconServer.SocketName),
            beam,
            logging.CreateLogger<BeaconServer>());

        var supervisor = new QuickshellSupervisor(
            new SystemProcessLauncher(),
            new GsettingsIconThemeSource(),
            new RestartPolicy(),
            logging.CreateLogger<QuickshellSupervisor>());

        var daemon = new OverworldDaemon(
            new ShellLocator(),
            server,
            beacon,
            supervisor,
            logging.CreateLogger<OverworldDaemon>());

        return await daemon.RunAsync(shutdown.Token);
    }
}
