using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Overworld.Web.Serving;

public sealed class DevServer : IDisposable
{
    public const string Folder = "OVERWORLD_WEB_DEV";

    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    private readonly Process _vite;

    private DevServer(Process vite, Uri address)
    {
        _vite = vite;
        Address = address;
    }

    public Uri Address { get; }

    public static DevServer? Start(Uri host)
    {
        if (Environment.GetEnvironmentVariable(Folder) is not { Length: > 0 } folder || !Directory.Exists(folder))
        {
            return null;
        }

        var port = FreePort();
        var start = new ProcessStartInfo("npm") { WorkingDirectory = folder, UseShellExecute = false };

        foreach (var argument in new[] { "run", "dev", "--", "--port", port.ToString(CultureInfo.InvariantCulture), "--strictPort", "--host", "127.0.0.1" })
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["OVERWORLD_HOST"] = host.GetLeftPart(UriPartial.Authority);

        Process? vite;

        try
        {
            vite = Process.Start(start);
        }
        catch (Win32Exception)
        {
            return null;
        }

        if (vite is null)
        {
            return null;
        }

        var deadline = DateTime.UtcNow + Patience;

        while (DateTime.UtcNow < deadline && !vite.HasExited)
        {
            if (Listening(port))
            {
                return new DevServer(vite, new Uri($"http://127.0.0.1:{port}/"));
            }

            Thread.Sleep(200);
        }

        Stop(vite);
        vite.Dispose();

        return null;
    }

    public void Dispose()
    {
        Stop(_vite);
        _vite.Dispose();
    }

    private static void Stop(Process vite)
    {
        try
        {
            if (!vite.HasExited)
            {
                vite.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static bool Listening(int port)
    {
        try
        {
            using var probe = new TcpClient();
            probe.Connect(IPAddress.Loopback, port);

            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();

        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }
}
