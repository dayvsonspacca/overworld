using Overworld.Allay.Agents;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Apps.Allay.Presentation.Tests.Fakes;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Allay.Presentation.Tests;

public sealed class AgentsViewModelTest : IDisposable
{
    private static readonly Blocked SignedOut = new("Signed out", "Sign in from a terminal.", "claude auth login");

    private readonly string _folder = Directory.CreateTempSubdirectory("allay-").FullName;
    private readonly FakeAgent _claude = new("claude-code", "Claude Code", new Ready("Ready", "Version 2"));
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
            Operation.From(new ConfigureAgentOp(_crew, _beacon)));
    }

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
        Assert.Equal("CC", Claude.Initials);
    }

    [Fact]
    public void ShowsAnAgentAsCheckingUntilItIsChecked()
    {
        var agent = new AgentViewModel(
            new AgentCard("codex", "Codex", "Writes code.", false, Setup.Default, []),
            Operation.From(new CheckAgentOp(_crew)),
            Operation.From(new HireAgentOp(_crew, _beacon)),
            Operation.From(new ConfigureAgentOp(_crew, _beacon)));

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

    private static async Task WaitUntilIdle(AgentViewModel agent)
    {
        while (agent.IsBusy)
        {
            await Task.Delay(10, Token);
        }
    }
}
