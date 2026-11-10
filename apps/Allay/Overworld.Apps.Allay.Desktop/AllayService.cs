using Microsoft.Extensions.Logging;
using Overworld.Allay.Bells;
using Overworld.Allay.Hiring;
using Overworld.Allay.Remote;
using Overworld.Beacon;
using Overworld.Bell;
using Overworld.Redstone;

namespace Overworld.Apps.Allay.Desktop;

internal static class AllayService
{
    public const int AlreadyRunning = 3;

    private const string From = "allay";

    public static async Task<int> RunAsync(Crew crew, Transcripts transcripts)
    {
        using var logging = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));
        using var stop = new CancellationTokenSource();
        using var terminated = System.Runtime.InteropServices.PosixSignalRegistration.Create(System.Runtime.InteropServices.PosixSignal.SIGTERM, signal =>
        {
            signal.Cancel = true;
            stop.Cancel();
        });
        Console.CancelKeyPress += (_, pressed) =>
        {
            pressed.Cancel = true;
            stop.Cancel();
        };

        var ringer = new Ringer(new BellClient(new RedstoneClient(SocketPath.Resolve())), From);
        using var host = new AllayHost(crew, transcripts, logging.CreateLogger<AllayHost>(), ringer: ringer);

        try
        {
            host.Start();
        }
        catch (AlreadyRunningException)
        {
            return AlreadyRunning;
        }

        _ = ringer.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(stop.Token), stop.Token);
        await host.RunAsync(stop.Token);

        return 0;
    }
}
