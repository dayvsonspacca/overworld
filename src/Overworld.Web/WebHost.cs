using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Overworld.Beacon;
using Overworld.Bedrock.Apps;
using Overworld.Redstone;
using Overworld.Web.Mirroring;
using Overworld.Web.Serving;
using Overworld.Web.Windowing;

namespace Overworld.Web;

public static class WebHost
{
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(2);

    public static int Run(AppIdentity app, Action<Page> show)
    {
        using var loop = new Loop();
        var page = new Page(new Mirror(), loop);
        page.Handle("open-link", Open);
        loop.Send(_ => show(page), null);

        using var server = new PageServer(page, Path.Combine(AppContext.BaseDirectory, "wwwroot"));
        server.Start();
        _ = FollowThemeAsync(page);

        using var window = WindowProcess.Open(app.Name, app.Title, server.Address!, Path.Combine(AppContext.BaseDirectory, "icon.png"));

        if (window is null)
        {
            Console.Error.WriteLine($"{app.Name}: overworld-glass could not be started; run just seed, or open {server.Address} by hand.");

            return 1;
        }

        window.WaitForExit();
        Leave(page);

        return 0;
    }

    private static void Open(JsonNode? asked)
    {
        if (asked?["url"]?.GetValue<string>() is { } url
            && Uri.TryCreate(url, UriKind.Absolute, out var link)
            && link.Scheme is "http" or "https" or "mailto")
        {
            try
            {
                using var opened = Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { link.ToString() }, UseShellExecute = false });
            }
            catch (Win32Exception)
            {
            }
        }
    }

    private static void Leave(Page page)
    {
        if (page.Leaving.Count == 0)
        {
            return;
        }

        using var patience = new CancellationTokenSource(Patience);
        var leaving = page.Loop.RunAsync(() => Task.WhenAll(page.Leaving.Select(leave => leave(patience.Token)))).Unwrap();
        Task.WaitAny([leaving], Patience);
    }

    private static async Task FollowThemeAsync(Page page)
    {
        var beacon = new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName));

        await foreach (var signal in beacon.WatchAsync(CancellationToken.None))
        {
            if (signal == "theme")
            {
                page.Emit("theme");
            }

            page.Emit("signal", signal);
        }
    }
}
