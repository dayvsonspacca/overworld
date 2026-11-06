using Microsoft.Extensions.Logging;
using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Bedrock.Operations;
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

        var allay = DataDirectory.For("allay");
        var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], new StaffFile(allay));

        return
        [
            Operation.From(new PingOp()),
            Operation.From(new ListPinsOp(places)),
            Operation.From(new AddPinOp(places)),
            Operation.From(new RemovePinOp(places)),
            Operation.From(new ListAgentsOp(crew)),
            Operation.From(new CheckAgentOp(crew)),
            Operation.From(new HireAgentOp(crew)),
            Operation.From(new ConfigureAgentOp(crew)),
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
