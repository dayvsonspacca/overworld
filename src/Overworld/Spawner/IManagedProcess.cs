namespace Overworld.Spawner;

internal interface IManagedProcess : IDisposable
{
    Task<int> WaitForExitAsync(CancellationToken token);

    Task TerminateAsync(TimeSpan grace);
}
