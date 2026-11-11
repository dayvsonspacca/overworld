using System.ComponentModel;
using System.Diagnostics;

namespace Overworld.Allay.ClaudeCode.Cli;

internal static class Launcher
{
    private static readonly string[] InheritedSession =
    [
        "CLAUDECODE",
        "CLAUDE_CODE_ENTRYPOINT",
        "CLAUDE_CODE_SESSION_ID",
        "CLAUDE_CODE_CHILD_SESSION",
        "CLAUDE_CODE_SESSION_ATTENDED",
        "CLAUDE_CODE_MESSAGING_SOCKET",
        "CLAUDE_CODE_MESSAGING_TOKEN",
        "CLAUDE_CODE_EXECPATH",
        "CLAUDE_PID",
    ];

    public static Process? Start(string executable, string workingDirectory, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var variable in InheritedSession)
        {
            start.Environment.Remove(variable);
        }

        start.Environment["CLAUDE_CODE_DISABLE_CLAUDE_MDS"] = "1";
        start.Environment["CLAUDE_CODE_DISABLE_AUTO_MEMORY"] = "1";

        try
        {
            return Process.Start(start);
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    public static async Task LetGoAsync(Process process, TimeSpan patience)
    {
        try
        {
            process.StandardInput.Close();
        }
        catch (IOException)
        {
        }

        using var waiting = new CancellationTokenSource(patience);

        try
        {
            await process.WaitForExitAsync(waiting.Token);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
        }
    }

    public static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
