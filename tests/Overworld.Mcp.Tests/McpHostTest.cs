using System.IO.Pipes;
using System.Text.Json.Nodes;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Mcp.Tests;

public sealed class McpHostTest : IAsyncDisposable
{
    public sealed record Greet(string Name);

    [Operation("greet", "Greets someone by name.")]
    private sealed class GreetOp : IOperation<Greet, string>
    {
        public Task<Result<string>> ExecuteAsync(Greet input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>($"Hello, {input.Name}!");
    }

    [Operation("wave", "Waves at everyone.")]
    private sealed class WaveOp : IOperation<Greet, string>
    {
        public Task<Result<string>> ExecuteAsync(Greet input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>("o/");
    }

    private readonly AnonymousPipeServerStream _toServer = new(PipeDirection.Out);

    private readonly AnonymousPipeServerStream _toClient = new(PipeDirection.In);

    private readonly StreamWriter _send;

    private readonly StreamReader _receive;

    private readonly Task _running;

    public McpHostTest()
    {
        var serverInput = new AnonymousPipeClientStream(PipeDirection.In, _toServer.ClientSafePipeHandle);
        var serverOutput = new AnonymousPipeClientStream(PipeDirection.Out, _toClient.ClientSafePipeHandle);

        _send = new StreamWriter(_toServer) { AutoFlush = true };
        _receive = new StreamReader(_toClient);

        _running = new McpHost("overworld:test", [Operation.From(new GreetOp()), Operation.From(new WaveOp())])
            .RunAsync(serverInput, serverOutput, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _toServer.DisposeAsync();
        await _running.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await _toClient.DisposeAsync();
    }

    [Fact]
    public async Task IntroducesItselfByTheAppCommand()
    {
        var initialized = await Initialize();

        Assert.Equal("overworld:test", initialized["result"]!["serverInfo"]!["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task OffersOneToolPerOperation()
    {
        await Initialize();

        var listed = await Request(2, "tools/list", new JsonObject());

        Assert.Equal(
            ["greet", "wave"],
            listed["result"]!["tools"]!.AsArray().Select(tool => tool!["name"]!.GetValue<string>()).Order());
    }

    [Fact]
    public async Task RunsTheOperationACallNames()
    {
        await Initialize();

        var called = await Request(3, "tools/call", new JsonObject
        {
            ["name"] = "greet",
            ["arguments"] = new JsonObject { ["name"] = "Steve" },
        });

        Assert.Equal("\"Hello, Steve!\"", called["result"]!["content"]![0]!["text"]!.GetValue<string>());
    }

    private async Task<JsonNode> Initialize()
    {
        var initialized = await Request(1, "initialize", new JsonObject
        {
            ["protocolVersion"] = "2025-06-18",
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject { ["name"] = "test", ["version"] = "1" },
        });

        await Send(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = "notifications/initialized" });

        return initialized;
    }

    private async Task<JsonNode> Request(int id, string method, JsonObject parameters)
    {
        await Send(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = parameters });

        while (true)
        {
            var line = await _receive.ReadLineAsync(TestContext.Current.CancellationToken)
                ?? throw new InvalidOperationException("The server closed before answering.");

            var message = JsonNode.Parse(line)!;

            if (message["id"]?.GetValue<int>() == id)
            {
                return message;
            }
        }
    }

    private Task Send(JsonObject message) => _send.WriteLineAsync(message.ToJsonString());
}
