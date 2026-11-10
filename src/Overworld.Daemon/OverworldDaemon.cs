using Microsoft.Extensions.Logging;
using Overworld.Beacon;
using Overworld.Daemon.Lifetime;
using Overworld.Daemon.Shell;
using Overworld.Daemon.Spawner;
using Overworld.Redstone;

namespace Overworld.Daemon;

internal sealed partial class OverworldDaemon(
    IShellLocator locator,
    RedstoneServer server,
    BeaconServer beacon,
    QuickshellSupervisor supervisor,
    AllaySupervisor allay,
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
        beacon.Start();

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        var serving = server.RunAsync(stop.Token);
        var shining = beacon.RunAsync(stop.Token);
        var agents = allay.RunAsync(stop.Token);

        try
        {
            await supervisor.RunAsync(shell, stop.Token);
        }
        finally
        {
            await stop.CancelAsync();
            await Task.WhenAll(serving, shining, agents);
        }
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "{Reason}")]
    private partial void LogFatal(string reason);
}
