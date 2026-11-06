using System.ComponentModel;
using System.Diagnostics;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Repositories;

public static class Git
{
    public static readonly TimeSpan Patience = TimeSpan.FromMinutes(2);

    public static async Task<Result<string>> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["GIT_TERMINAL_PROMPT"] = "0";

        if (!start.Environment.ContainsKey("GIT_SSH_COMMAND"))
        {
            start.Environment["GIT_SSH_COMMAND"] = "ssh -o BatchMode=yes";
        }

        Process? process;

        try
        {
            process = Process.Start(start);
        }
        catch (Win32Exception)
        {
            return new Error("git.missing", "git is not installed or not on the PATH.");
        }

        if (process is null)
        {
            return new Error("git.failed", "git did not start.");
        }

        using (process)
        using (var patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            patience.CancelAfter(Patience);

            var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var error = process.StandardError.ReadToEndAsync(CancellationToken.None);

            try
            {
                await process.WaitForExitAsync(patience.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);

                return new Error("git.stopped", cancellationToken.IsCancellationRequested ? "git was stopped." : "git took too long and was stopped.");
            }

            if (process.ExitCode != 0)
            {
                var said = (await error).Trim();

                return new Error("git.failed", said.Length > 0 ? said.Split('\n')[^1].Trim() : $"git failed with code {process.ExitCode}.");
            }

            return await output;
        }
    }
}
