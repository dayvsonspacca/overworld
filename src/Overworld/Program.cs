using Microsoft.Extensions.Logging;
using Overworld.Bedrock.Cli;
using Overworld.Bedrock.Mcp;
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
    private const string Command = "overworld";

    private static Task<int> Main(string[] args) =>
        args.Length == 0 ? RunDaemonAsync() : RunClientAsync(args);

    private static Operation[] Operations() =>
    [
        Operation.From(new PingOp()),
    ];

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
            new RestartPolicy(),
            logging.CreateLogger<QuickshellSupervisor>());

        var daemon = new OverworldDaemon(
            new ShellLocator(),
            server,
            supervisor,
            logging.CreateLogger<OverworldDaemon>());

        return await daemon.RunAsync(shutdown.Token);
    }

    private static async Task<int> RunClientAsync(string[] args)
    {
        var client = new RedstoneClient(SocketPath.Resolve());
        Operation[] operations = [.. Operations().Select(operation => new RemoteOperation(operation, client))];

        if (args is ["mcp"])
        {
            await new McpHost(Command, operations)
                .RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput(), CancellationToken.None);

            return 0;
        }

        return await new CliHost(Command, operations)
            .RunAsync(args, Console.Out, Console.Error, CancellationToken.None);
    }
}
