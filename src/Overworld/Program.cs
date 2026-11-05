using Microsoft.Extensions.Logging;
using Overworld.Bedrock.Operations;
using Overworld.Daemon;
using Overworld.Lifetime;
using Overworld.Logging;
using Overworld.Redstone;
using Overworld.Shell;
using Overworld.Spawner;

namespace Overworld;

internal static class Program
{
    private static async Task<int> Main()
    {
        using var logging = LoggerFactory.Create(builder => builder.AddOverworldConsole());
        using var shutdown = new ShutdownSignal();

        Operation[] operations =
        [
            Operation.From(new PingOp()),
        ];

        using var server = new RedstoneServer(
            SocketPath.Resolve(),
            new Dispatcher(operations, logging.CreateLogger<Dispatcher>()),
            logging.CreateLogger<RedstoneServer>());

        var supervisor = new QuickshellSupervisor(
            new SystemProcessLauncher(),
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
