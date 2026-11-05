namespace Overworld.Daemon.Spawner;

internal interface IManagedProcess : IDisposable
{
    Task<int> WaitForExitAsync(CancellationToken token);

    Task TerminateAsync(TimeSpan grace);
}
