using Microsoft.Extensions.Logging;
using Overworld.Lifetime;
using Overworld.Redstone;
using Overworld.Shell;
using Overworld.Spawner;

namespace Overworld.Daemon;

internal sealed partial class OverworldDaemon(
    IShellLocator locator,
    RedstoneServer server,
    QuickshellSupervisor supervisor,
    ILogger<OverworldDaemon> logger)
{
    private const int AlreadyRunning = 3;

    public async Task<int> RunAsync(CancellationToken token)
    {
        try
        {
            await ServeAsync(token);
            return 0;
        }
        catch (OverworldException exception)
        {
            LogFatal(exception.Message);
            return exception.ExitCode;
        }
        catch (AlreadyRunningException exception)
        {
            LogFatal(exception.Message);
            return AlreadyRunning;
        }
    }

    private async Task ServeAsync(CancellationToken token)
    {
        var shell = locator.Find();
        server.Start();

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        var serving = server.RunAsync(stop.Token);

        try
        {
            await supervisor.RunAsync(shell, stop.Token);
        }
        finally
        {
            await stop.CancelAsync();
            await serving;
        }
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "{Reason}")]
    private partial void LogFatal(string reason);
}
