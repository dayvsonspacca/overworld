namespace Overworld.Daemon.Spawner;

internal interface IIconThemeSource
{
    Task<string?> FindAsync(CancellationToken token);
}
