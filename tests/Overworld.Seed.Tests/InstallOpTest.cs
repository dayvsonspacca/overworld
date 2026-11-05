using Overworld.Bedrock.Results;
using Overworld.Seed.Steps;

namespace Overworld.Seed.Tests;

public sealed class InstallOpTest
{
    private sealed class FakeStep(Result<string> result) : IStep
    {
        public bool Ran { get; private set; }

        public Task<Result<string>> RunAsync(CancellationToken cancellationToken)
        {
            Ran = true;

            return Task.FromResult(result);
        }
    }

    private static Task<Result<IReadOnlyList<string>>> Install(params IStep[] steps) =>
        new InstallOp(steps).ExecuteAsync(new Install(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task RunsEveryStepInOrderAndTellsWhatEachDid()
    {
        var installed = await Install(new FakeStep("linked hypr"), new FakeStep("published overworld"));

        Assert.Equal(["linked hypr", "published overworld"], installed.Value);
    }

    [Fact]
    public async Task StopsAtTheFirstStepThatFails()
    {
        var after = new FakeStep("linked overworld");

        var installed = await Install(
            new FakeStep("linked hypr"),
            new FakeStep(new Error("seed.publish_failed", "The build broke.")),
            after);

        Assert.Equal("seed.publish_failed", installed.Error.Code);
        Assert.Equal("The build broke. Done before it: linked hypr.", installed.Error.Message);
        Assert.False(after.Ran);
    }

    [Fact]
    public async Task KeepsTheErrorAsIsWhenNothingWasDone()
    {
        var installed = await Install(new FakeStep(new Error("seed.taken", "Taken.")));

        Assert.Equal("Taken.", installed.Error.Message);
    }
}
