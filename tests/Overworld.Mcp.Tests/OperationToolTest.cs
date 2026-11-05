using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Mcp.Tests;

public sealed class OperationToolTest
{
    public sealed record Greet(
        [property: Description("Who to greet.")] string Name,
        [property: Description("How to greet them.")] string Greeting = "Hello");

    public sealed record Greeted(string Text);

    [Operation("greet", "Greets someone by name.")]
    private sealed class GreetOp : IOperation<Greet, Greeted>
    {
        public Task<Result<Greeted>> ExecuteAsync(Greet input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<Greeted>>(input.Name == "Herobrine"
                ? new Error("greet.unknown", "Herobrine is not here.")
                : new Greeted($"{input.Greeting}, {input.Name}!"));
    }

    private readonly OperationTool _tool = new(Operation.From(new GreetOp()));

    [Fact]
    public void IsNamedAndDescribedLikeItsOperation()
    {
        Assert.Equal("greet", _tool.ProtocolTool.Name);
        Assert.Equal("Greets someone by name.", _tool.ProtocolTool.Description);
    }

    [Fact]
    public void DescribesItsInputAsAJsonSchema()
    {
        var schema = JsonNode.Parse(_tool.ProtocolTool.InputSchema.GetRawText())!;

        Assert.Equal("object", schema["type"]!.GetValue<string>());
        Assert.Equal(["name"], schema["required"]!.AsArray().Select(name => name!.GetValue<string>()));
        Assert.Equal("Who to greet.", schema["properties"]!["name"]!["description"]!.GetValue<string>());
        Assert.Equal("string", schema["properties"]!["name"]!["type"]!.GetValue<string>());
        Assert.Equal("How to greet them.", schema["properties"]!["greeting"]!["description"]!.GetValue<string>());
    }

    [Fact]
    public async Task AnswersWithWhatTheOperationReturnsAsJson()
    {
        var result = await Call("""{"name":"Steve","greeting":"Hi"}""");

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("Hi, Steve!", JsonNode.Parse(TextOf(result))!["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task ReportsAFailedOperationAsAToolError()
    {
        var result = await Call("""{"name":"Herobrine"}""");

        Assert.True(result.IsError);
        Assert.Equal("greet.unknown: Herobrine is not here.", TextOf(result));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"name":null}""")]
    [InlineData("""{"name":3}""")]
    public async Task ReportsAnInputThatDoesNotFitAsAToolError(string arguments)
    {
        var result = await Call(arguments);

        Assert.True(result.IsError);
        Assert.StartsWith("input.invalid: ", TextOf(result));
    }

    private async Task<CallToolResult> Call(string arguments)
    {
        await using var server = McpServer.Create(new StreamServerTransport(Stream.Null, Stream.Null), new McpServerOptions());

        var request = new RequestContext<CallToolRequestParams>(
            server,
            new JsonRpcRequest { Method = "tools/call" },
            new CallToolRequestParams
            {
                Name = "greet",
                Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments),
            });

        return await _tool.InvokeAsync(request, TestContext.Current.CancellationToken);
    }

    private static string TextOf(CallToolResult result) => Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
}
