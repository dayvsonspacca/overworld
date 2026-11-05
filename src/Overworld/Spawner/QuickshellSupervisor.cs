using Microsoft.Extensions.Logging;
using Overworld.Shell;

namespace Overworld.Spawner;

internal sealed partial class QuickshellSupervisor(
    IShellLocator locator,
    IProcessLauncher launcher,
    RestartPolicy policy,
    ILogger<QuickshellSupervisor> logger)
{
    private const string Executable = "qs";
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(2);

    public async Task RunAsync(CancellationToken token)
    {
        var shell = locator.Find();

        while (true)
        {
            int code;
            try
            {
                code = await RunOnceAsync(shell, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (code == 0)
            {
                LogExited();
                return;
            }

            policy.RecordCrash();
            if (!policy.CanRestart)
                throw new QuickshellCrashLoopException(code);

            LogRestarting(code);
        }
    }

    private async Task<int> RunOnceAsync(string shell, CancellationToken token)
    {
        using var quickshell = Start(shell);
        LogStarted(shell);

        try
        {
            return await quickshell.WaitForExitAsync(token);
        }
        catch (OperationCanceledException)
        {
            LogStopping();
            await quickshell.TerminateAsync(Grace);
            throw;
        }
    }

    private IManagedProcess Start(string shell)
    {
        try
        {
            return launcher.Start(Executable, ["-p", shell]);
        }
        catch (ProcessStartException exception)
        {
            throw new QuickshellNotInstalledException(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Quickshell started with shell {Shell}")]
    private partial void LogStarted(string shell);

    [LoggerMessage(Level = LogLevel.Information, Message = "Quickshell exited normally")]
    private partial void LogExited();

    [LoggerMessage(Level = LogLevel.Information, Message = "Stop requested, terminating Quickshell")]
    private partial void LogStopping();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Quickshell crashed (exit code {ExitCode}), restarting")]
    private partial void LogRestarting(int exitCode);
}
