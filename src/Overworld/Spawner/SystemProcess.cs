using System.Diagnostics;

namespace Overworld.Spawner;

internal sealed class SystemProcess(Process process) : IManagedProcess
{
    public async Task<int> WaitForExitAsync(CancellationToken token)
    {
        await process.WaitForExitAsync(token);
        return process.ExitCode;
    }

    public async Task TerminateAsync(TimeSpan grace)
    {
        if (process.HasExited) return;

        LibC.Kill(process.Id, LibC.SIGTERM);

        using var timeout = new CancellationTokenSource(grace);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
    }

    public void Dispose() => process.Dispose();
}
