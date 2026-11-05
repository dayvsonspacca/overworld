using System.ComponentModel;
using System.Diagnostics;

namespace Overworld.Daemon.Spawner;

internal sealed class GsettingsIconThemeSource : IIconThemeSource
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public async Task<string?> FindAsync(CancellationToken token)
    {
        var info = new ProcessStartInfo("gsettings")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("get");
        info.ArgumentList.Add("org.gnome.desktop.interface");
        info.ArgumentList.Add("icon-theme");

        using var process = new Process { StartInfo = info };
        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(Timeout);
        try
        {
            var output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            var theme = output.Trim().Trim('\'');
            return process.ExitCode == 0 && theme.Length > 0 ? theme : null;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            process.Kill();
            return null;
        }
    }
}
