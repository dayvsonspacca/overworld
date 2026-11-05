using System.Net.Sockets;
using System.Text.Json;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone;

internal sealed class RedstoneClient(string path) : IRedstoneClient
{
    public async Task<Response> SendAsync(Request request, CancellationToken token)
    {
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), token);
        }
        catch (SocketException)
        {
            return Response.Failure(new Error("daemon.not_running", $"The overworld daemon is not running (nothing answers on {path})."));
        }

        await using var stream = new NetworkStream(socket);
        using var reader = new StreamReader(stream);
        await using var writer = new StreamWriter(stream) { AutoFlush = true };

        var json = JsonSerializer.Serialize(request, RedstoneJsonContext.Default.Request);
        await writer.WriteLineAsync(json.AsMemory(), token);

        var line = await reader.ReadLineAsync(token);
        if (line is null)
            return Invalid("the daemon closed the connection without answering");

        try
        {
            return JsonSerializer.Deserialize(line, RedstoneJsonContext.Default.Response)
                ?? Invalid("the daemon sent an empty response");
        }
        catch (JsonException)
        {
            return Invalid("the daemon sent a malformed response");
        }
    }

    private static Response Invalid(string reason) =>
        Response.Failure(new Error("daemon.invalid_response", $"No usable answer: {reason}."));
}
