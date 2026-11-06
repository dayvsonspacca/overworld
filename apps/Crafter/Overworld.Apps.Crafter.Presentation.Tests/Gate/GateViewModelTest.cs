using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Apps.Crafter.Presentation.Gate;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Crafter.Presentation.Tests.Gate;

public sealed class GateViewModelTest : IDisposable
{
    private sealed class Agent(string id, Readiness readiness) : IAgent
    {
        public AgentId Id { get; } = new(id);

        public string Name => $"Agent {id}";

        public string Summary => "Works.";

        public IReadOnlyList<string> InstructionFiles => [];

        public Readiness Readiness { get; set; } = readiness;

        public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ModelOption>>([]);

        public Task<Readiness> CheckAsync(CancellationToken cancellationToken) => Task.FromResult(Readiness);

        public ISession Begin(Brief brief, string? resume = null) => throw new NotSupportedException();
    }

    private static readonly Ready Ready = new("Ready", "Version 1");

    private readonly string _folder = Directory.CreateTempSubdirectory("crafter-gate-").FullName;
    private readonly Agent _claude = new("claude-code", Ready);
    private readonly Agent _codex = new("codex", Ready);
    private readonly Crew _crew;
    private bool _opens = true;
    private int _opened;
    private readonly GateViewModel _gate;

    public GateViewModelTest()
    {
        _crew = new Crew([_claude, _codex], new StaffFile(_folder));
        _gate = new GateViewModel(Operation.From(new ListAgentsOp(_crew)), Operation.From(new CheckAgentOp(_crew)), () =>
        {
            _opened++;

            return _opens;
        });
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task ShutsCrafterWhenNobodyIsHired()
    {
        await _gate.LoadAsync(Token);

        Assert.True(_gate.IsShut);
        Assert.True(_gate.NobodyHired);
        Assert.Equal("Crafter needs an agent", _gate.Heading);
    }

    [Fact]
    public async Task OpensCrafterWhenAHiredAgentIsReady()
    {
        await _crew.HireAsync("claude-code", true, Token);

        await _gate.LoadAsync(Token);

        Assert.False(_gate.IsShut);
    }

    [Fact]
    public async Task ShutsCrafterWhenNoHiredAgentIsReadyAndSaysWhy()
    {
        await _crew.HireAsync("claude-code", true, Token);
        _claude.Readiness = new Blocked("Signed out", "Sign in from a terminal.", "claude auth login");

        await _gate.LoadAsync(Token);

        Assert.True(_gate.IsShut);
        Assert.False(_gate.NobodyHired);
        Assert.Equal("Agent claude-code", _gate.Agent);
        Assert.Equal("Signed out · Sign in from a terminal.", _gate.Trouble);
        Assert.Equal("Your agent can't work yet", _gate.Heading);
    }

    [Fact]
    public async Task OpensCrafterWhenAnyHiredAgentIsReady()
    {
        await _crew.HireAsync("claude-code", true, Token);
        await _crew.HireAsync("codex", true, Token);
        _claude.Readiness = new Blocked("Signed out", "Sign in from a terminal.");

        await _gate.LoadAsync(Token);

        Assert.False(_gate.IsShut);
    }

    [Fact]
    public async Task OpensOnItsOwnOnceAnAgentIsHired()
    {
        await _gate.LoadAsync(Token);
        await _crew.HireAsync("claude-code", true, Token);

        await _gate.FollowAsync(Signals("theme", Crew.Signal), Token);

        Assert.False(_gate.IsShut);
    }

    [Fact]
    public async Task IgnoresOtherSignals()
    {
        await _gate.LoadAsync(Token);
        await _crew.HireAsync("claude-code", true, Token);

        await _gate.FollowAsync(Signals("theme", "pins"), Token);

        Assert.True(_gate.IsShut);
    }

    [Fact]
    public async Task ChecksAgainOnRequest()
    {
        await _crew.HireAsync("claude-code", true, Token);
        _claude.Readiness = new Blocked("Signed out", "Sign in from a terminal.");
        await _gate.LoadAsync(Token);
        _claude.Readiness = Ready;

        await _gate.LoadCommand.ExecuteAsync(null);

        Assert.False(_gate.IsShut);
    }

    [Fact]
    public void OpensAllayAndSaysSoWhenItCannot()
    {
        _gate.OpenAllayCommand.Execute(null);

        Assert.Equal(1, _opened);
        Assert.Null(_gate.Problem);

        _opens = false;
        _gate.OpenAllayCommand.Execute(null);

        Assert.Contains("Allay isn't installed", _gate.Problem);
    }

    private static async IAsyncEnumerable<string> Signals(params string[] signals)
    {
        foreach (var signal in signals)
        {
            await Task.Yield();

            yield return signal;
        }
    }
}
