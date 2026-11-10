using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace Overworld.Web.Serving;

public sealed class PageServer : IDisposable
{
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(16);

    private static readonly HashSet<string> Pictures = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg" };

    private readonly HttpListener _listener = new();

    private readonly Page _page;

    private readonly StaticFiles _files;

    private readonly string _key = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    private readonly List<Channel<string>> _sockets = [];

    private readonly HashSet<string> _origins = [];

    private readonly Timer _flushing;

    private bool _scheduled;

    public PageServer(Page page, string root)
    {
        _page = page;
        _files = new StaticFiles(root);
        _flushing = new Timer(_ => page.Loop.Post(Flush));
        page.Mirror.Changed += (_, _) => Schedule();
        page.Emitted += (_, emitted) => page.Loop.Post(() =>
        {
            Flush();
            Send(new JsonArray(new JsonObject { ["t"] = "event", ["name"] = emitted.Name, ["data"] = emitted.Data?.DeepClone() }));
        });
    }

    public Uri? Address { get; private set; }

    public void Start()
    {
        var port = FreePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        var debug = Environment.GetEnvironmentVariable("OVERWORLD_WEB_DEBUG") is { Length: > 0 } ? "&debug=1" : string.Empty;
        var tiles = Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE") is { Length: > 0 } ? "&tiles=1" : string.Empty;
        Address = new Uri($"http://127.0.0.1:{port}/?key={_key}{debug}{tiles}");
        _ = ListenAsync();
    }

    public void Allow(Uri origin) => _origins.Add(origin.GetLeftPart(UriPartial.Authority));

    public void Dispose()
    {
        _flushing.Dispose();
        _listener.Close();
    }

    private static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();

        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    private void Schedule()
    {
        if (_scheduled)
        {
            return;
        }

        _scheduled = true;
        _flushing.Change(Settle, Timeout.InfiniteTimeSpan);
    }

    private void Flush()
    {
        _scheduled = false;
        var batch = _page.Mirror.Drain();

        if (batch.Count > 0)
        {
            Send(batch);
        }
    }

    private void Send(JsonArray batch)
    {
        var text = batch.ToJsonString();

        lock (_sockets)
        {
            foreach (var socket in _sockets)
            {
                socket.Writer.TryWrite(text);
            }
        }
    }

    private async Task ListenAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception closed) when (closed is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(context));
        }
    }

    private async Task ServeAsync(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "/";

        try
        {
            if (path == "/ws")
            {
                await TalkAsync(context);

                return;
            }

            if (path == "/theme.css")
            {
                await ReplyAsync(context, "text/css; charset=utf-8", Encoding.UTF8.GetBytes(Theme()));

                return;
            }

            if (path == "/blob")
            {
                await ServeBlobAsync(context);

                return;
            }

            if (path == "/file")
            {
                await ServeLocalAsync(context);

                return;
            }

            if (_files.Find(path) is { } file)
            {
                await ReplyAsync(context, StaticFiles.Kind(file), await File.ReadAllBytesAsync(file));

                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }
        catch (Exception broken) when (broken is HttpListenerException or IOException or WebSocketException or ObjectDisposedException)
        {
            context.Response.Abort();
        }
    }

    private async Task ServeBlobAsync(HttpListenerContext context)
    {
        var bytes = context.Request.QueryString["key"] == _key && int.TryParse(context.Request.QueryString["id"], out var id)
            ? await _page.Loop.RunAsync(() => _page.Mirror.Find(id) as byte[])
            : null;

        if (bytes is null)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();

            return;
        }

        context.Response.Headers["Cache-Control"] = "max-age=31536000, immutable";
        await ReplyAsync(context, Sniff(bytes), bytes);
    }

    private static string Sniff(byte[] bytes) => bytes switch
    {
        [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
        [0xFF, 0xD8, ..] => "image/jpeg",
        [0x47, 0x49, 0x46, ..] => "image/gif",
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => "image/webp",
        _ => "application/octet-stream",
    };

    private async Task ServeLocalAsync(HttpListenerContext context)
    {
        var local = context.Request.QueryString["path"];

        if (context.Request.QueryString["key"] != _key || local is null || !Pictures.Contains(Path.GetExtension(local)) || !File.Exists(local))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();

            return;
        }

        await ReplyAsync(context, Path.GetExtension(local).ToUpperInvariant() switch
        {
            ".PNG" => "image/png",
            ".WEBP" => "image/webp",
            ".GIF" => "image/gif",
            ".SVG" => "image/svg+xml",
            _ => "image/jpeg",
        }, await File.ReadAllBytesAsync(local));
    }

    private static string Theme()
    {
        try
        {
            return ThemeSheet.Css(ThemeSheet.Worn);
        }
        catch (Exception unreadable) when (unreadable is IOException or InvalidDataException or System.Text.Json.JsonException or InvalidOperationException or FormatException)
        {
            return ":root { color-scheme: dark; }";
        }
    }

    private static async Task ReplyAsync(HttpListenerContext context, string kind, byte[] body)
    {
        context.Response.ContentType = kind;

        if (context.Response.Headers["Cache-Control"] is null)
        {
            context.Response.Headers["Cache-Control"] = "no-store";
        }

        context.Response.ContentLength64 = body.Length;
        await context.Response.OutputStream.WriteAsync(body);
        context.Response.Close();
    }

    private async Task TalkAsync(HttpListenerContext context)
    {
        var origin = context.Request.Headers["Origin"];
        var expected = $"http://127.0.0.1:{context.Request.Url!.Port}";

        if (!context.Request.IsWebSocketRequest || context.Request.QueryString["key"] != _key || (origin is not null && origin != expected && !_origins.Contains(origin)))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();

            return;
        }

        var accepted = await context.AcceptWebSocketAsync(null);
        using var socket = accepted.WebSocket;
        var outgoing = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

        var whole = await _page.Loop.RunAsync(() => _page.Mirror.Whole().ToJsonString());
        outgoing.Writer.TryWrite(whole);

        lock (_sockets)
        {
            _sockets.Add(outgoing);
        }

        var writing = WriteAsync(socket, outgoing.Reader);

        try
        {
            await ReadAsync(socket, outgoing.Writer);
        }
        finally
        {
            lock (_sockets)
            {
                _sockets.Remove(outgoing);
            }

            outgoing.Writer.TryComplete();
            await writing;
        }
    }

    private static async Task WriteAsync(WebSocket socket, ChannelReader<string> reader)
    {
        try
        {
            await foreach (var text in reader.ReadAllAsync())
            {
                await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            }
        }
        catch (Exception closed) when (closed is WebSocketException or ObjectDisposedException)
        {
        }
    }

    private async Task ReadAsync(WebSocket socket, ChannelWriter<string> replies)
    {
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();

        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult received;

            try
            {
                received = await socket.ReceiveAsync(buffer, CancellationToken.None);
            }
            catch (Exception closed) when (closed is WebSocketException or ObjectDisposedException)
            {
                return;
            }

            if (received.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            message.Write(buffer, 0, received.Count);

            if (!received.EndOfMessage)
            {
                continue;
            }

            var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            message.SetLength(0);

            if (Environment.GetEnvironmentVariable("OVERWORLD_WEB_TRACE") is { Length: > 0 })
            {
                await Console.Error.WriteLineAsync($"trace: {text[..Math.Min(text.Length, 300)]}");
            }

            if (JsonNode.Parse(text) is JsonObject asked)
            {
                _page.Loop.Post(() => _ = AnswerAsync(asked, replies));
            }
        }
    }

    private async Task AnswerAsync(JsonObject asked, ChannelWriter<string> replies)
    {
        var mirror = _page.Mirror;
        var id = asked["id"]?.GetValue<int>() ?? 0;
        JsonNode? value = null;

        switch (asked["t"]?.GetValue<string>())
        {
            case "call":
                mirror.Call(id, asked["cmd"]!.GetValue<string>(), asked["arg"]);
                break;
            case "set":
                mirror.Set(id, asked["prop"]!.GetValue<string>(), asked["value"]);
                break;
            case "invoke":
                value = await mirror.InvokeAsync(id, asked["method"]!.GetValue<string>(), asked["args"] as JsonArray ?? []);
                break;
            case "ask":
                value = await _page.AskAsync(asked["name"]!.GetValue<string>(), asked["arg"]);
                break;
            case "focus":
                _page.IsActive = asked["value"]?.GetValue<bool>() ?? false;
                break;
            case "log":
                await Console.Error.WriteLineAsync($"page: {asked["text"]?.GetValue<string>()}");
                break;
        }

        if (asked["n"] is { } number)
        {
            Flush();
            replies.TryWrite(new JsonArray(new JsonObject { ["t"] = "reply", ["n"] = number.DeepClone(), ["value"] = value?.DeepClone() }).ToJsonString());
        }
    }
}
