using System.Text.Json.Nodes;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Tests.Operations;

public sealed class OperationTest
{
    public sealed record Greet(string Name);

    [Operation("greet", "Greets someone by name.")]
    private sealed class GreetOp : IOperation<Greet, string>
    {
        public Task<Result<string>> ExecuteAsync(Greet input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>(input.Name.Length == 0
                ? new Error("greet.nobody", "Nobody to greet.")
                : $"Hello, {input.Name}!");
    }

    private sealed class UnnamedOp : IOperation<Greet, string>
    {
        public Task<Result<string>> ExecuteAsync(Greet input, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    [Operation("Greet_Loudly", "Shouts.")]
    private sealed class BadlyNamedOp : IOperation<Greet, string>
    {
        public Task<Result<string>> ExecuteAsync(Greet input, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    [Operation("count", "Counts how many run at once.")]
    private sealed class CountOp : IOperation<Greet, int>
    {
        private int _running;

        public int MostAtOnce { get; private set; }

        public async Task<Result<int>> ExecuteAsync(Greet input, CancellationToken cancellationToken)
        {
            MostAtOnce = Math.Max(MostAtOnce, Interlocked.Increment(ref _running));
            await Task.Delay(20, cancellationToken);
            Interlocked.Decrement(ref _running);

            return 0;
        }
    }

    [Fact]
    public async Task RunsOneOperationAtATime()
    {
        var count = new CountOp();
        var operation = Operation.From(count);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            Task.Run(() => operation.ExecuteAsync(new Greet("Steve"), TestContext.Current.CancellationToken))));

        Assert.Equal(1, count.MostAtOnce);
    }

    [Fact]
    public void TakesItsNameAndDescriptionFromTheAttribute()
    {
        var operation = Operation.From(new GreetOp());

        Assert.Equal("greet", operation.Name);
        Assert.Equal("Greets someone by name.", operation.Description);
        Assert.Equal(typeof(Greet), operation.InputType);
    }

    [Fact]
    public async Task PassesTheValueOfASuccessThrough()
    {
        var result = await Operation.From(new GreetOp()).ExecuteAsync(new Greet("Steve"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello, Steve!", result.Value);
    }

    [Fact]
    public async Task PassesTheErrorOfAFailureThrough()
    {
        var result = await Operation.From(new GreetOp()).ExecuteAsync(new Greet(""), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("greet.nobody", result.Error.Code);
    }

    [Fact]
    public async Task RunsTheSameThroughItsUntypedShape()
    {
        Operation operation = Operation.From(new GreetOp());

        var success = await operation.ExecuteAsync(new Greet("Alex"), TestContext.Current.CancellationToken);
        var failure = await operation.ExecuteAsync(new Greet(""), TestContext.Current.CancellationToken);

        Assert.Equal("Hello, Alex!", success.Value);
        Assert.Equal("greet.nobody", failure.Error.Code);
    }

    [Fact]
    public async Task RunsOneOperationAtATimeWhicheverShapeCallsIt()
    {
        var count = new CountOp();
        var typed = Operation.From(count);
        Operation untyped = typed;

        await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Task.Run(() => i % 2 == 0
            ? (Task)typed.ExecuteAsync(new Greet("Steve"), TestContext.Current.CancellationToken)
            : untyped.ExecuteAsync(new Greet("Steve"), TestContext.Current.CancellationToken))));

        Assert.Equal(1, count.MostAtOnce);
    }

    [Fact]
    public async Task RunsFromJsonAndAnswersInJson()
    {
        var result = await Operation.From(new GreetOp())
            .RunAsync(new JsonObject { ["name"] = "Steve" }, TestContext.Current.CancellationToken);

        Assert.Equal("Hello, Steve!", result.Value!.GetValue<string>());
    }

    [Fact]
    public async Task RunsNothingWhenTheJsonIsNotItsInput()
    {
        var count = new CountOp();

        var result = await Operation.From(count)
            .RunAsync(new JsonObject { ["name"] = 42 }, TestContext.Current.CancellationToken);

        Assert.Equal(BedrockJson.InvalidInput, result.Error.Code);
        Assert.Equal(0, count.MostAtOnce);
    }

    [Fact]
    public async Task PassesTheErrorOfAFailureThroughJson()
    {
        var result = await Operation.From(new GreetOp())
            .RunAsync(new JsonObject { ["name"] = "" }, TestContext.Current.CancellationToken);

        Assert.Equal("greet.nobody", result.Error.Code);
    }

    [Fact]
    public void RefusesAnOperationWithoutTheAttribute()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Operation.From(new UnnamedOp()));

        Assert.Contains("UnnamedOp", thrown.Message);
    }

    [Fact]
    public void RefusesANameThatIsNotKebabCase()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Operation.From(new BadlyNamedOp()));

        Assert.Contains("'Greet_Loudly'", thrown.Message);
    }
}
