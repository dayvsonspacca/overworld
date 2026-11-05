using Microsoft.Extensions.Logging;
using Overworld.Desktop;

namespace Overworld.Spawner;

internal sealed partial class QuickshellSupervisor(
    IProcessLauncher launcher,
    IIconThemeSource icons,
    RestartPolicy policy,
    ILogger<QuickshellSupervisor> logger)
{
    private const string Executable = "qs";
    private const string IconThemeVariable = "QS_ICON_THEME";
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(2);

    public async Task RunAsync(string shell, CancellationToken token)
    {
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
        var environment = await EnvironmentAsync(token);
        using var quickshell = Start(shell, environment);
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

    private async Task<IReadOnlyDictionary<string, string>> EnvironmentAsync(CancellationToken token)
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(IconThemeVariable)))
            return new Dictionary<string, string>();

        var theme = await icons.FindAsync(token);
        if (theme is null)
        {
            LogNoIconTheme();
            return new Dictionary<string, string>();
        }

        LogIconTheme(theme);
        return new Dictionary<string, string> { [IconThemeVariable] = theme };
    }

    private IManagedProcess Start(string shell, IReadOnlyDictionary<string, string> environment)
    {
        try
        {
            return launcher.Start(Executable, ["-p", shell], environment);
        }
        catch (ProcessStartException exception)
        {
            throw new QuickshellNotInstalledException(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Quickshell started with shell {Shell}")]
    private partial void LogStarted(string shell);

    [LoggerMessage(Level = LogLevel.Information, Message = "Using icon theme {Theme}")]
    private partial void LogIconTheme(string theme);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No icon theme found; Quickshell falls back to hicolor")]
    private partial void LogNoIconTheme();

    [LoggerMessage(Level = LogLevel.Information, Message = "Quickshell exited normally")]
    private partial void LogExited();

    [LoggerMessage(Level = LogLevel.Information, Message = "Stop requested, terminating Quickshell")]
    private partial void LogStopping();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Quickshell crashed (exit code {ExitCode}), restarting")]
    private partial void LogRestarting(int exitCode);
}
