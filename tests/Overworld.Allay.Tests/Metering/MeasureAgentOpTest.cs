using Overworld.Allay.Hiring;
using Overworld.Allay.Metering;
using Overworld.Allay.Tests.Fakes;

namespace Overworld.Allay.Tests.Metering;

public sealed class MeasureAgentOpTest : IDisposable
{
    private static readonly Usage Measured = new([new Limit("Current session", 40)], new Spending("Extra usage this month", 2.5m, "USD", 10m));

    private readonly string _folder = Directory.CreateTempSubdirectory("allay-").FullName;
    private readonly Crew _crew;

    public MeasureAgentOpTest() =>
        _crew = new Crew([new MeteredAgent("claude-code", Measured), new ScriptedAgent("codex")], new StaffFile(_folder));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task TellsAnAgentsUsage()
    {
        Assert.Equal(Measured, (await new MeasureAgentOp(_crew).ExecuteAsync(new MeasureAgent(" claude-code "), Token)).Value);
    }

    [Fact]
    public async Task SaysWhenAnAgentDoesNotTellItsUsage()
    {
        var usage = await new MeasureAgentOp(_crew).ExecuteAsync(new MeasureAgent("codex"), Token);

        Assert.Equal("agent.unmetered", usage.Error.Code);
        Assert.Equal("Agent codex doesn't tell its usage.", usage.Error.Message);
    }

    [Fact]
    public async Task SaysWhenThereIsNoSuchAgent()
    {
        Assert.Equal("agent.unknown", (await new MeasureAgentOp(_crew).ExecuteAsync(new MeasureAgent("gemini"), Token)).Error.Code);
    }

    [Fact]
    public async Task CardsSayWhichAgentsTellTheirUsageAndTheirColor()
    {
        var cards = (await _crew.CardsAsync(Token)).Value;

        Assert.Equal([true, false], cards.Select(card => card.Metered));
        Assert.Equal(["#D97757", "#5FD3F3"], cards.Select(card => card.Color));
    }
}
