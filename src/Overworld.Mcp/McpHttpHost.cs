using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Overworld.Bedrock.Json;

namespace Overworld.Mcp;

public sealed class McpHttpHost(string name) : IDisposable
{
    private static readonly string[] Versions = ["2025-06-18", "2025-03-26", "2024-11-05"];

    private readonly HttpListener _listener = new();

    private readonly CancellationTokenSource _closing = new();

    private readonly ConcurrentDictionary<string, Caller> _callers = new();

    public Uri? Endpoint { get; private set; }

    public void Start()
    {
        var port = FreePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/mcp/");
        _listener.Start();
        Endpoint = new Uri($"http://127.0.0.1:{port}/mcp");
        _ = ListenAsync();
    }

    public string Admit(Caller caller)
    {
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24));
        _callers[token] = caller;

        return token;
    }

    public void Release(string token) => _callers.TryRemove(token, out _);

    public void Dispose()
    {
        _closing.Cancel();

        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _closing.Dispose();
    }

    internal async Task<JsonNode?> AnswerAsync(string body, Caller caller)
    {
        JsonNode? message;

        try
        {
            message = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return Error(null, -32700, "Parse error");
        }

        if (message is JsonArray batch)
        {
            var answers = new JsonArray();

            foreach (var each in batch)
            {
                if (each is JsonObject single && await ReplyAsync(single, caller) is { } answer)
                {
                    answers.Add(answer);
                }
            }

            return answers.Count > 0 ? answers : null;
        }

        return message is JsonObject one ? await ReplyAsync(one, caller) : Error(null, -32600, "Invalid Request");
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        return port;
    }

    private async Task ListenAsync()
    {
        while (!_closing.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception stopped) when (stopped is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(context));
        }
    }

    private async Task ServeAsync(HttpListenerContext context)
    {
        var response = context.Response;

        try
        {
            var request = context.Request;

            if (request.Headers["Origin"] is not null)
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;

                return;
            }

            var authorization = request.Headers["Authorization"];
            var token = authorization is not null && authorization.StartsWith("Bearer ", StringComparison.Ordinal) ? authorization["Bearer ".Length..].Trim() : null;

            if (token is null || !_callers.TryGetValue(token, out var caller))
            {
                response.StatusCode = (int)HttpStatusCode.Unauthorized;

                return;
            }

            if (request.HttpMethod != "POST")
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                response.AddHeader("Allow", "POST");

                return;
            }

            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var answer = await AnswerAsync(await reader.ReadToEndAsync(_closing.Token), caller);

            if (answer is null)
            {
                response.StatusCode = (int)HttpStatusCode.Accepted;

                return;
            }

            var body = Encoding.UTF8.GetBytes(answer.ToJsonString(BedrockJson.Options));
            response.ContentType = "application/json";
            response.ContentLength64 = body.Length;
            await response.OutputStream.WriteAsync(body, _closing.Token);
        }
        catch (Exception broken) when (broken is HttpListenerException or IOException or OperationCanceledException or ObjectDisposedException)
        {
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch (Exception gone) when (gone is HttpListenerException or ObjectDisposedException)
            {
            }
        }
    }

    private async Task<JsonNode?> ReplyAsync(JsonObject message, Caller caller)
    {
        var id = message["id"]?.DeepClone();
        var method = message["method"] is JsonValue value && value.TryGetValue<string>(out var named) ? named : null;

        if (id is null)
        {
            return null;
        }

        return method switch
        {
            "initialize" => Result(id, Initialize(message["params"] as JsonObject, caller)),
            "ping" => Result(id, new JsonObject()),
            "tools/list" => Result(id, new JsonObject { ["tools"] = new JsonArray([.. caller.Tools.Select(tool => tool.Describe())]) }),
            "tools/call" => Result(id, await CallAsync(message["params"] as JsonObject, caller)),
            _ => Error(id, -32601, $"Method not found: {method}"),
        };
    }

    private JsonObject Initialize(JsonObject? parameters, Caller caller)
    {
        var asked = parameters?["protocolVersion"] is JsonValue value && value.TryGetValue<string>(out var version) ? version : null;
        var result = new JsonObject
        {
            ["protocolVersion"] = asked is not null && Versions.Contains(asked) ? asked : Versions[0],
            ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = name,
                ["version"] = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0",
            },
        };

        if (caller.Instructions is { } told)
        {
            result["instructions"] = told;
        }

        return result;
    }

    private async Task<JsonObject> CallAsync(JsonObject? parameters, Caller caller)
    {
        var wanted = parameters?["name"] is JsonValue value && value.TryGetValue<string>(out var named) ? named : null;

        if (caller.Tools.FirstOrDefault(tool => tool.Name == wanted) is not { } tool)
        {
            return Content($"There is no tool called {wanted}.", failed: true);
        }

        var result = await tool.Operation.RunAsync(tool.Arguments(parameters?["arguments"] as JsonObject), _closing.Token);

        return result.IsSuccess
            ? Content(result.Value?.ToJsonString(BedrockJson.Options) ?? "null", failed: false)
            : Content($"{result.Error.Code}: {result.Error.Message}", failed: true);
    }

    private static JsonObject Content(string text, bool failed) => new()
    {
        ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text }),
        ["isError"] = failed,
    };

    private static JsonObject Result(JsonNode id, JsonNode result) =>
        new() { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result };

    private static JsonObject Error(JsonNode? id, int code, string message) =>
        new() { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JsonObject { ["code"] = code, ["message"] = message } };
}
