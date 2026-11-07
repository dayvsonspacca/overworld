using System.ComponentModel;
using System.Diagnostics;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

public sealed class GsettingsColorScheme : IColorScheme
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public async Task<Result<string>> PreferAsync(Variant variant, CancellationToken cancellationToken)
    {
        var scheme = variant == Variant.Light ? "prefer-light" : "prefer-dark";
        var info = new ProcessStartInfo("gsettings") { RedirectStandardError = true };
        info.ArgumentList.Add("set");
        info.ArgumentList.Add("org.gnome.desktop.interface");
        info.ArgumentList.Add("color-scheme");
        info.ArgumentList.Add(scheme);

        using var process = new Process { StartInfo = info };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return new Error("gtk.no_gsettings", "gsettings is not installed; run just seed so GTK apps learn the theme's variant.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            var error = await process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            return process.ExitCode == 0
                ? scheme
                : new Error("gtk.scheme_failed", $"gsettings could not set the color scheme: {error.Trim()}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill();

            return new Error("gtk.scheme_failed", "gsettings did not answer in time.");
        }
    }
}
