namespace Overworld.Desktop;

internal interface IIconThemeSource
{
    Task<string?> FindAsync(CancellationToken token);
}
