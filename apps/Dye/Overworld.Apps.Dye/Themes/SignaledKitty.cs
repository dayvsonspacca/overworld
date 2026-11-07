using System.ComponentModel;
using System.Diagnostics;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

public sealed class SignaledKitty : IKitty
{
    private const int NoneRunning = 1;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public async Task<Result<string>> ReloadAsync(CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo("pkill") { RedirectStandardError = true };
        info.ArgumentList.Add("--signal");
        info.ArgumentList.Add("USR1");
        info.ArgumentList.Add("--exact");
        info.ArgumentList.Add("kitty");

        using var process = new Process { StartInfo = info };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return new Error("kitty.no_pkill", "pkill is not installed, so open kitty windows wear the theme only when reopened.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            var error = await process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            return process.ExitCode switch
            {
                0 => "kitty reloaded",
                NoneRunning => "no kitty is open",
                _ => new Error("kitty.reload_failed", $"kitty could not be signaled to reload: {error.Trim()}"),
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill();

            return new Error("kitty.reload_failed", "pkill did not answer in time.");
        }
    }
}
