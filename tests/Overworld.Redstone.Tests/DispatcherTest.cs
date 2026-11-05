using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Bedrock.Operations;

namespace Overworld.Redstone.Tests;

public sealed class DispatcherTest
{
    private readonly Dispatcher _dispatcher = new(
        [Operation.From(new EchoOp()), Operation.From(new ExplodeOp())],
        NullLogger<Dispatcher>.Instance);

    private Task<Response> Dispatch(string operation, JsonNode? input) =>
        _dispatcher.DispatchAsync(new Request(operation, input), TestContext.Current.CancellationToken);

    [Fact]
    public async Task AnswersWithTheResultOfTheOperation()
    {
        var response = await Dispatch("echo", new JsonObject { ["word"] = "redstone" });

        Assert.Null(response.Error);
        Assert.Equal("redstone", response.Result!.GetValue<string>());
    }

    [Fact]
    public async Task AnswersWithTheErrorOfTheOperation()
    {
        var response = await Dispatch("echo", new JsonObject { ["word"] = "" });

        Assert.Equal("echo.silence", response.Error!.Code);
    }

    [Fact]
    public async Task RefusesAnOperationItDoesNotHave()
    {
        var response = await Dispatch("mine", new JsonObject());

        Assert.Equal("operation.unknown", response.Error!.Code);
    }

    [Fact]
    public async Task ReadsAMissingInputAsAnEmptyObject()
    {
        var response = await Dispatch("echo", null);

        Assert.Equal("input.invalid", response.Error!.Code);
    }

    [Fact]
    public async Task TurnsACrashIntoAnError()
    {
        var response = await Dispatch("explode", new JsonObject { ["word"] = "tnt" });

        Assert.Equal("operation.crashed", response.Error!.Code);
    }
}
