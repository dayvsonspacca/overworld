using System.ComponentModel;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Cli.Tests;

public sealed class CliHostTest : IDisposable
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

    public sealed record Nothing;

    [Operation("wave", "Waves at everyone.")]
    private sealed class WaveOp : IOperation<Nothing, string>
    {
        public Task<Result<string>> ExecuteAsync(Nothing input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>("o/");
    }

    private readonly StringWriter _output = new();

    private readonly StringWriter _errors = new();

    public void Dispose()
    {
        _output.Dispose();
        _errors.Dispose();
    }

    private Task<int> Run(params string[] args) =>
        new CliHost("overworld:test", [Operation.From(new GreetOp()), Operation.From(new WaveOp())])
            .RunAsync(args, _output, _errors, TestContext.Current.CancellationToken);

    [Fact]
    public async Task PrintsWhatTheOperationReturnsAsJson()
    {
        Assert.Equal(0, await Run("greet", "--name", "Steve"));
        Assert.Equal("{\n  \"text\": \"Hello, Steve!\"\n}\n", _output.ToString().ReplaceLineEndings("\n"));
        Assert.Empty(_errors.ToString());
    }

    [Fact]
    public async Task PassesOptionalOptionsWhenGiven()
    {
        await Run("greet", "--name", "Alex", "--greeting", "Hi");

        Assert.Contains("Hi, Alex!", _output.ToString());
    }

    [Fact]
    public async Task ReportsAFailedOperationOnStandardError()
    {
        Assert.Equal(1, await Run("greet", "--name", "Herobrine"));
        Assert.Equal("greet.unknown: Herobrine is not here.", _errors.ToString().Trim());
        Assert.Empty(_output.ToString());
    }

    [Fact]
    public async Task RefusesAnOperationItDoesNotHave()
    {
        Assert.Equal(2, await Run("mine"));
        Assert.StartsWith("cli.unknown_operation: There is no operation 'mine'.", _errors.ToString());
    }

    [Fact]
    public async Task PointsToTheHelpWhenOptionsDoNotFit()
    {
        Assert.Equal(2, await Run("greet"));
        Assert.Contains("cli.missing_option: --name is required.", _errors.ToString());
        Assert.Contains("Run overworld:test greet --help", _errors.ToString());
    }

    [Theory]
    [InlineData]
    [InlineData("--help")]
    public async Task ListsItsOperations(params string[] args)
    {
        Assert.Equal(0, await Run(args));
        Assert.Contains("Usage: overworld:test <operation> [options]", _output.ToString());
        Assert.Contains("greet  Greets someone by name.", _output.ToString());
    }

    [Fact]
    public async Task DescribesTheOptionsOfAnOperation()
    {
        Assert.Equal(0, await Run("greet", "--help"));

        var help = _output.ToString();
        Assert.Contains("Usage: overworld:test greet [options]", help);
        Assert.Contains("--name <text>      Required. Who to greet.", help);
        Assert.Contains("--greeting <text>  How to greet them.", help);
    }

    [Fact]
    public async Task RunsAnOperationThatTakesNoOptions()
    {
        Assert.Equal(0, await Run("wave"));
        Assert.Equal("\"o/\"", _output.ToString().Trim());
    }

    [Fact]
    public async Task LeavesOutTheOptionsOfAnOperationThatHasNone()
    {
        await Run("wave", "--help");

        Assert.StartsWith("Usage: overworld:test wave\n", _output.ToString().ReplaceLineEndings("\n"));
        Assert.DoesNotContain("Options:", _output.ToString());
    }

    [Fact]
    public void RefusesTwoOperationsWithTheSameName()
    {
        var thrown = Assert.Throws<ArgumentException>(() =>
            new CliHost("overworld:test", [Operation.From(new GreetOp()), Operation.From(new GreetOp())]));

        Assert.Contains("'greet'", thrown.Message);
    }
}
