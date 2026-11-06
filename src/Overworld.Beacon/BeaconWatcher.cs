using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Overworld.Beacon;

public sealed class BeaconWatcher(string path, TimeSpan retry)
{
    public BeaconWatcher(string path)
        : this(path, TimeSpan.FromSeconds(2))
    {
    }

    public async IAsyncEnumerable<string> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            using (var socket = await ConnectAsync(cancellationToken))
            {
                if (socket is not null)
                {
                    using var reader = new StreamReader(new NetworkStream(socket));

                    while (await ReadAsync(reader, cancellationToken) is { } line)
                    {
                        if (Signal(line) is { } signal)
                        {
                            yield return signal;
                        }
                    }
                }
            }

            await Task.Delay(retry, cancellationToken);
        }
    }

    private async Task<Socket?> ConnectAsync(CancellationToken cancellationToken)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken);

            return socket;
        }
        catch (SocketException)
        {
            socket.Dispose();

            return null;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<string?> ReadAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadLineAsync(cancellationToken);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string? Signal(string line)
    {
        try
        {
            return JsonNode.Parse(line)?["signal"] is JsonValue value && value.TryGetValue<string>(out var signal)
                ? signal
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
