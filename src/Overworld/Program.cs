using Microsoft.Extensions.Logging;
using Overworld.Bedrock.Hosting;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Daemon;
using Overworld.Desktop;
using Overworld.Lifetime;
using Overworld.Logging;
using Overworld.Pins;
using Overworld.Redstone;
using Overworld.Shell;
using Overworld.Spawner;

namespace Overworld;

internal static class Program
{
    private const string Command = "overworld";

    private static Task<int> Main(string[] args)
    {
        var client = new RedstoneClient(SocketPath.Resolve());

        return AppHost.RunAsync(
            Command,
            [.. Operations().Select(operation => new RemoteOperation(operation, client))],
            args,
            withoutArgs: RunDaemonAsync);
    }

    private static Operation[] Operations()
    {
        var pins = DataDirectory.For("pins");
        var places = new PinPlaces(new Dictionary<string, PinStore>
        {
            ["menu"] = new(Path.Combine(pins, "menu.json")),
            ["taskbar"] = new(Path.Combine(pins, "taskbar.json")),
        });

        return
        [
            Operation.From(new PingOp()),
            Operation.From(new ListPinsOp(places)),
            Operation.From(new AddPinOp(places)),
            Operation.From(new RemovePinOp(places)),
        ];
    }

    private static async Task<int> RunDaemonAsync()
    {
        using var logging = LoggerFactory.Create(builder => builder.AddOverworldConsole());
        using var shutdown = new ShutdownSignal();

        using var server = new RedstoneServer(
            SocketPath.Resolve(),
            new Dispatcher(Operations(), logging.CreateLogger<Dispatcher>()),
            logging.CreateLogger<RedstoneServer>());

        var supervisor = new QuickshellSupervisor(
            new SystemProcessLauncher(),
            new GsettingsIconThemeSource(),
            new RestartPolicy(),
            logging.CreateLogger<QuickshellSupervisor>());

        var daemon = new OverworldDaemon(
            new ShellLocator(),
            server,
            supervisor,
            logging.CreateLogger<OverworldDaemon>());

        return await daemon.RunAsync(shutdown.Token);
    }
}
