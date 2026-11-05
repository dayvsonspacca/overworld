using Microsoft.Extensions.Logging;
using Overworld.Lifetime;
using Overworld.Logging;
using Overworld.Shell;
using Overworld.Spawner;

namespace Overworld;

internal static partial class Program
{
    private static async Task<int> Main()
    {
        using var logging = LoggerFactory.Create(builder => builder.AddOverworldConsole());
        var logger = logging.CreateLogger("Overworld");

        using var shutdown = new ShutdownSignal();

        var supervisor = new QuickshellSupervisor(
            new ShellLocator(),
            new SystemProcessLauncher(),
            new RestartPolicy(),
            logging.CreateLogger<QuickshellSupervisor>());

        try
        {
            await supervisor.RunAsync(shutdown.Token);
            return 0;
        }
        catch (OverworldException exception)
        {
            LogFatal(logger, exception.Message);
            return exception.ExitCode;
        }
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "{Reason}")]
    private static partial void LogFatal(ILogger logger, string reason);
}
