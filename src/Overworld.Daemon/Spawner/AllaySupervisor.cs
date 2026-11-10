using Microsoft.Extensions.Logging;

namespace Overworld.Daemon.Spawner;

internal sealed partial class AllaySupervisor(IProcessLauncher launcher, RestartPolicy policy, ILogger<AllaySupervisor> logger)
{
    private const string Executable = "overworld:allay";
    private const int AlreadyRunning = 3;
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            int code;

            try
            {
                code = await RunOnceAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ProcessStartException)
            {
                LogNotInstalled();
                return;
            }

            if (code == AlreadyRunning)
            {
                LogAlreadyRunning();
                await PauseAsync(Wait, token);
                continue;
            }

            policy.RecordCrash();

            if (!policy.CanRestart)
            {
                LogGivingUp(code);
                return;
            }

            LogRestarting(code);
        }
    }

    private async Task<int> RunOnceAsync(CancellationToken token)
    {
        using var allay = launcher.Start(Executable, ["serve"], new Dictionary<string, string>());
        LogStarted();

        try
        {
            return await allay.WaitForExitAsync(token);
        }
        catch (OperationCanceledException)
        {
            LogStopping();
            await allay.TerminateAsync(Grace);
            throw;
        }
    }

    private static async Task PauseAsync(TimeSpan wait, CancellationToken token)
    {
        try
        {
            await Task.Delay(wait, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Allay started serving agents")]
    private partial void LogStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Stop requested, letting Allay finish")]
    private partial void LogStopping();

    [LoggerMessage(Level = LogLevel.Information, Message = "Allay is already serving; checking again later")]
    private partial void LogAlreadyRunning();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Allay is not installed; agents run inside the apps until it is")]
    private partial void LogNotInstalled();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Allay stopped (exit code {ExitCode}), restarting")]
    private partial void LogRestarting(int exitCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Allay keeps stopping (exit code {ExitCode}); giving up until the daemon restarts")]
    private partial void LogGivingUp(int exitCode);
}
