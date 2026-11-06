using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Overworld.Beacon.Tests;

public sealed class BeaconServerTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("beacon-").FullName;

    private string Path => System.IO.Path.Combine(_folder, "overworld-beacon.sock");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task SendsEachSignalAsALineOfJson()
    {
        var beam = new Beam();
        using var server = new BeaconServer(Path, beam, NullLogger<BeaconServer>.Instance);
        server.Start();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var shining = server.RunAsync(stop.Token);

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(Path), Token);
        using var reader = new StreamReader(new NetworkStream(socket));
        var line = reader.ReadLineAsync(Token).AsTask();

        while (!line.IsCompleted)
        {
            beam.Light("pins");
            await Task.WhenAny(line, Task.Delay(20, Token));
        }

        await stop.CancelAsync();
        await shining;

        Assert.Equal("{\"signal\":\"pins\"}", await line);
    }

    [Fact]
    public void TakesOverASocketLeftBehind()
    {
        File.WriteAllText(Path, "");

        using var server = new BeaconServer(Path, new Beam(), NullLogger<BeaconServer>.Instance);
        server.Start();

        Assert.True(File.Exists(Path));
    }

    [Fact]
    public void RemovesItsSocketWhenDisposed()
    {
        var server = new BeaconServer(Path, new Beam(), NullLogger<BeaconServer>.Instance);
        server.Start();

        server.Dispose();

        Assert.False(File.Exists(Path));
    }
}
