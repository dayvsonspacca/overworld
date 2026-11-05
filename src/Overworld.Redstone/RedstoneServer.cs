using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone;

public sealed partial class RedstoneServer(
    string path,
    Dispatcher dispatcher,
    ILogger<RedstoneServer> logger) : IDisposable
{
    private static readonly Error InvalidRequest =
        new("request.invalid", "A request is one line of JSON like {\"operation\":\"ping\",\"input\":{}}.");

    private readonly UnixDomainSocketEndPoint _endpoint = new(path);
    private Socket? _listener;

    public void Start()
    {
        if (File.Exists(path))
        {
            if (IsAnswering())
                throw new AlreadyRunningException(path);

            File.Delete(path);
        }

        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            listener.Bind(_endpoint);
            listener.Listen();
        }
        catch
        {
            listener.Dispose();
            throw;
        }

        _listener = listener;
        LogListening(path);
    }

    public async Task RunAsync(CancellationToken token)
    {
        var listener = _listener ?? throw new InvalidOperationException("Start the server before running it.");

        try
        {
            while (true)
            {
                var client = await listener.AcceptAsync(token);
                _ = ServeAsync(client, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (_listener is null) return;

        _listener.Dispose();
        File.Delete(path);
    }

    private bool IsAnswering()
    {
        using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            probe.Connect(_endpoint);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private async Task ServeAsync(Socket client, CancellationToken token)
    {
        await using var stream = new NetworkStream(client, ownsSocket: true);
        using var reader = new StreamReader(stream);
        await using var writer = new StreamWriter(stream) { AutoFlush = true };

        try
        {
            while (await reader.ReadLineAsync(token) is { } line)
            {
                var response = await HandleAsync(line, token);
                var json = JsonSerializer.Serialize(response, RedstoneJsonContext.Default.Response);
                await writer.WriteLineAsync(json.AsMemory(), token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException exception)
        {
            LogClientFailed(exception);
        }
    }

    private async Task<Response> HandleAsync(string line, CancellationToken token)
    {
        Request? request;
        try
        {
            request = JsonSerializer.Deserialize(line, RedstoneJsonContext.Default.Request);
        }
        catch (JsonException)
        {
            return Response.Failure(InvalidRequest);
        }

        return request is null
            ? Response.Failure(InvalidRequest)
            : await dispatcher.DispatchAsync(request, token);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Listening on {Path}")]
    private partial void LogListening(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Client connection failed")]
    private partial void LogClientFailed(Exception exception);
}
