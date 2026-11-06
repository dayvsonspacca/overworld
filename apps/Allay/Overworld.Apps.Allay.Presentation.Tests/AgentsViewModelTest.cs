using Overworld.Allay.Agents;
using Overworld.Allay.Hiring;
using Overworld.Allay.Metering;
using Overworld.Allay.Settings;
using Overworld.Apps.Allay.Presentation.Tests.Fakes;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Allay.Presentation.Tests;

public sealed class AgentsViewModelTest : IDisposable
{
    private static readonly Blocked SignedOut = new("Signed out", "Sign in from a terminal.", "claude auth login");

    private readonly string _folder = Directory.CreateTempSubdirectory("allay-").FullName;
    private readonly MeteredAgent _claude = new("claude-code", "Claude Code", new Ready("Ready", "Version 2"));
    private readonly FakeAgent _codex = new("codex", "Codex", SignedOut);
    private readonly BeaconClient _beacon = new(new Daemon());
    private readonly Crew _crew;
    private readonly AgentsViewModel _agents;

    public AgentsViewModelTest()
    {
        _crew = new Crew([_claude, _codex], new StaffFile(_folder));
        _agents = new AgentsViewModel(
            Operation.From(new ListAgentsOp(_crew)),
            Operation.From(new CheckAgentOp(_crew)),
            Operation.From(new HireAgentOp(_crew, _beacon)),
            Operation.From(new ConfigureAgentOp(_crew, _beacon)),
            Operation.From(new MeasureAgentOp(_crew)),
            Clock);
    }

    private static readonly Clock Clock = new(new DateTimeOffset(2026, 11, 6, 17, 56, 0, TimeSpan.Zero));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private AgentViewModel Claude => _agents.Agents[0];

    private AgentViewModel Codex => _agents.Agents[1];

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task ListsEveryAgentSelectsTheFirstAndChecksThemAll()
    {
        await _agents.LoadAsync(Token);

        Assert.Equal(["Claude Code", "Codex"], _agents.Agents.Select(agent => agent.Name));
        Assert.Same(Claude, _agents.Selected);
        Assert.Equal("0 of 2 hired", _agents.Headcount);
        Assert.True(Claude.IsReady);
        Assert.Equal("Not hired · Ready", Claude.Standing);
        Assert.True(Codex.IsBlocked);
        Assert.Equal("claude auth login", Codex.Remedy);
        Assert.Equal("#D97757", Claude.Color);
    }

    [Fact]
    public void ShowsAnAgentAsCheckingUntilItIsChecked()
    {
        var agent = new AgentViewModel(
            new AgentCard("codex", "Codex", "Writes code.", "#5FD3F3", false, false, Setup.Default, []),
            Operation.From(new CheckAgentOp(_crew)),
            Operation.From(new HireAgentOp(_crew, _beacon)),
            Operation.From(new ConfigureAgentOp(_crew, _beacon)),
            Operation.From(new MeasureAgentOp(_crew)),
            Clock);

        Assert.True(agent.IsChecking);
        Assert.Equal("Checking…", agent.State);
        Assert.False(agent.HireCommand.CanExecute(null));
    }

    [Fact]
    public async Task OffersToHireOnlyAnAgentThatIsReady()
    {
        await _agents.LoadAsync(Token);

        Assert.True(Claude.HireCommand.CanExecute(null));
        Assert.False(Codex.HireCommand.CanExecute(null));
    }

    [Fact]
    public async Task HiresAnAgentForEveryApp()
    {
        await _agents.LoadAsync(Token);

        await Claude.HireCommand.ExecuteAsync(null);

        Assert.True(Claude.IsHired);
        Assert.False(Claude.HireCommand.CanExecute(null));
        Assert.True(Claude.LetGoCommand.CanExecute(null));
        Assert.Equal("1 of 2 hired", _agents.Headcount);
        Assert.Equal([_claude], (await _crew.HiredAsync(Token)).Value);
    }

    [Fact]
    public async Task SaysWhyItCouldNotHireAnAgentThatStoppedBeingReady()
    {
        await _agents.LoadAsync(Token);
        _claude.Readiness = SignedOut;

        await Claude.HireCommand.ExecuteAsync(null);

        Assert.False(Claude.IsHired);
        Assert.Contains("can only be hired once it is ready", Claude.Problem);
    }

    [Fact]
    public async Task LetsGoAnAgentThatBrokeAfterBeingHired()
    {
        await _agents.LoadAsync(Token);
        await Claude.HireCommand.ExecuteAsync(null);
        _claude.Readiness = SignedOut;
        await Claude.CheckCommand.ExecuteAsync(null);

        Assert.True(Claude.IsBlocked);
        Assert.True(Claude.LetGoCommand.CanExecute(null));

        await Claude.LetGoCommand.ExecuteAsync(null);

        Assert.False(Claude.IsHired);
        Assert.Equal("0 of 2 hired", _agents.Headcount);
    }

    [Fact]
    public async Task ChecksEveryAgentAgain()
    {
        await _agents.LoadAsync(Token);
        _codex.Readiness = new Ready("Ready", "Version 1");

        await _agents.CheckAllCommand.ExecuteAsync(null);

        Assert.True(Codex.IsReady);
        Assert.True(Codex.HireCommand.CanExecute(null));
    }

    [Fact]
    public async Task SetsUpHowAHiredAgentStarts()
    {
        await _agents.LoadAsync(Token);
        await Claude.HireCommand.ExecuteAsync(null);

        await Claude.PickModeCommand.ExecuteAsync(Mode.Plan);
        await Claude.PickEffortCommand.ExecuteAsync(Effort.Max);
        Claude.Model = Claude.Models.Single(option => option.Id == "small");
        await WaitUntilIdle(Claude);

        Assert.True(Claude.IsPlan);
        Assert.Equal("Only plans.", Claude.ModeHint);
        Assert.True(Claude.IsMax);
        Assert.Equal("small", Claude.Model?.Id);
        Assert.Equal(new Setup(Mode.Plan, "small", Effort.Max), (await _crew.SetupOfAsync("claude-code", Token)).Value);
    }

    [Fact]
    public async Task OffersTheAgentsOwnModelFirstAndChoosesItAtFirst()
    {
        await _agents.LoadAsync(Token);

        Assert.Equal([Crew.DefaultModel, "big", "small"], Claude.Models.Select(option => option.Id));
        Assert.Equal(Crew.DefaultModel, Claude.Model?.Id);
    }

    [Fact]
    public async Task KeepsTheSelectedAgentWhenLoadingAgain()
    {
        await _agents.LoadAsync(Token);
        _agents.Selected = Codex;

        await _agents.LoadAsync(Token);

        Assert.Equal("codex", _agents.Selected?.Id);
    }

    [Fact]
    public async Task ShowsTheUsageOfAReadyAgentThatTellsIt()
    {
        _claude.Usage = new Usage(
        [
            new Limit("Current session", 94, new DateTimeOffset(2026, 11, 6, 18, 34, 0, TimeSpan.Zero), Close: true),
            new Limit("This week · all models", 50, new DateTimeOffset(2026, 11, 7, 9, 59, 0, TimeSpan.Zero)),
            new Limit("This week · Fable", 0),
        ]);

        await _agents.LoadAsync(Token);

        Assert.True(Claude.ShowsUsage);
        Assert.Equal(
            [
                new LimitRow("Current session", 94, "Resets in 38 min", true),
                new LimitRow("This week · all models", 50, "Resets tomorrow at 9:59 AM", false),
                new LimitRow("This week · Fable", 0, null, false),
            ],
            Claude.Usage!.Limits);
        Assert.Equal("94%", Claude.Usage.Limits[0].Used);
        Assert.Equal("Checked at 5:56 PM", Claude.Usage.Checked);
        Assert.False(Claude.Usage.IsBare);
    }

    [Theory]
    [InlineData(2026, 11, 6, 21, 49, "Resets today at 9:49 PM")]
    [InlineData(2026, 11, 10, 7, 0, "Resets Tue at 7:00 AM")]
    [InlineData(2026, 11, 20, 7, 0, "Resets Nov 20 at 7:00 AM")]
    public async Task SaysWhenALimitResets(int year, int month, int day, int hour, int minute, string resets)
    {
        _claude.Usage = new Usage([new Limit("Current session", 10, new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero))]);

        await _agents.LoadAsync(Token);

        Assert.Equal(resets, Claude.Usage!.Limits[0].Resets);
    }

    [Fact]
    public async Task ShowsWhatAnAgentSpentAgainstItsCap()
    {
        _claude.Usage = new Usage([], new Spending("Extra usage this month", 12.40m, "USD", 50m));

        await _agents.LoadAsync(Token);

        Assert.Equal(new SpendingRow("Extra usage this month", "$12.40", "of $50.00", 24.8), Claude.Usage!.Spending);
    }

    [Fact]
    public async Task ShowsWhatAnAgentSpentWithoutACap()
    {
        _claude.Usage = new Usage([], new Spending("Spent this month", 3.21m, "BRL"));

        await _agents.LoadAsync(Token);

        Assert.Equal(new SpendingRow("Spent this month", "3.21 BRL", null, null), Claude.Usage!.Spending);
    }

    [Fact]
    public async Task SaysWhyItCouldNotTellTheUsage()
    {
        _claude.Usage = new Bedrock.Results.Error("agent.usage_unknown", "Claude Code couldn't tell its usage: it timed out.");

        await _agents.LoadAsync(Token);

        Assert.Equal("Claude Code couldn't tell its usage: it timed out.", Claude.Usage!.Problem);
        Assert.Empty(Claude.Usage.Limits);
        Assert.Null(Claude.Usage.Checked);
        Assert.False(Claude.Usage.IsBare);
    }

    [Fact]
    public async Task SaysWhenAnAgentReportsNoUsage()
    {
        await _agents.LoadAsync(Token);

        Assert.True(Claude.Usage!.IsBare);
    }

    [Fact]
    public async Task ShowsNoUsageForAnAgentThatDoesNotTellItOrIsNotReady()
    {
        await _agents.LoadAsync(Token);

        Assert.Null(Codex.Usage);
        Assert.False(Codex.ShowsUsage);

        _claude.Readiness = SignedOut;
        await Claude.CheckCommand.ExecuteAsync(null);

        Assert.False(Claude.ShowsUsage);
    }

    private static async Task WaitUntilIdle(AgentViewModel agent)
    {
        while (agent.IsBusy)
        {
            await Task.Delay(10, Token);
        }
    }
}
