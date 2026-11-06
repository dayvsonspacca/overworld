using System.Text.Json;
using Overworld.Allay.Agents;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Allay.Tests.Fakes;
using Overworld.Bedrock.Json;

namespace Overworld.Allay.Tests.Hiring;

public sealed class CrewTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("allay-").FullName;
    private readonly ScriptedAgent _claude = new("claude-code");
    private readonly ScriptedAgent _codex = new("codex", new Blocked("Signed out", "Codex is not signed in.", "codex login"), models: []);
    private readonly Crew _crew;

    public CrewTest() => _crew = new Crew([_claude, _codex], new StaffFile(_folder));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string AgentsFile => Path.Combine(_folder, "agents.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private Task<Bedrock.Results.Result<AgentCard>> ConfigureAsync(string agent, Mode? mode = null, string? model = null, Effort? effort = null) =>
        new ConfigureAgentOp(_crew).ExecuteAsync(new ConfigureAgent(agent, mode, model, effort), Token);

    [Fact]
    public async Task ListsEveryAgentUnhiredInAutoModeAtFirst()
    {
        var cards = (await new ListAgentsOp(_crew).ExecuteAsync(new ListAgents(), Token)).Value;

        Assert.Equal(["claude-code", "codex"], cards.Select(card => card.Id));
        Assert.All(cards, card => Assert.False(card.Hired));
        Assert.All(cards, card => Assert.Equal(new Setup(Mode.Auto, null, Effort.Default), card.Setup));
        Assert.Equal(["big", "small"], cards[0].Models.Select(model => model.Id));
        Assert.False(File.Exists(AgentsFile));
    }

    [Fact]
    public async Task HiresAnAgentAndLetsItGo()
    {
        Assert.True((await new HireAgentOp(_crew).ExecuteAsync(new HireAgent("claude-code"), Token)).Value.Hired);
        Assert.Equal([_claude], (await _crew.HiredAsync(Token)).Value);

        Assert.False((await new HireAgentOp(_crew).ExecuteAsync(new HireAgent("claude-code", Hired: false), Token)).Value.Hired);
        Assert.Empty((await _crew.HiredAsync(Token)).Value);
    }

    [Fact]
    public async Task SetsUpAnAgentForEveryAppChangingOnlyWhatIsGiven()
    {
        await ConfigureAsync("claude-code", mode: Mode.Plan, model: "big");

        var configured = await ConfigureAsync("claude-code", effort: Effort.Max);

        Assert.Equal(new Setup(Mode.Plan, "big", Effort.Max), configured.Value.Setup);
        Assert.Equal(new Setup(Mode.Plan, "big", Effort.Max), (await new Crew([_claude], new StaffFile(_folder)).SetupOfAsync("claude-code", Token)).Value);
    }

    [Fact]
    public async Task GoesBackToTheAgentsOwnModel()
    {
        await ConfigureAsync("claude-code", model: "big");

        var configured = await ConfigureAsync("claude-code", model: Crew.DefaultModel);

        Assert.Null(configured.Value.Setup.Model);
        Assert.DoesNotContain("setups", await File.ReadAllTextAsync(AgentsFile, Token));
    }

    [Fact]
    public async Task RefusesAModelTheAgentDoesNotOffer()
    {
        var configured = await ConfigureAsync("claude-code", model: "huge");

        Assert.Equal("agent.model_unknown", configured.Error.Code);
        Assert.Contains("big, small, or default", configured.Error.Message);
    }

    [Fact]
    public async Task TakesAnyModelFromAnAgentThatListsNone()
    {
        Assert.Equal("gpt-6", (await ConfigureAsync("codex", model: "gpt-6")).Value.Setup.Model);
    }

    [Fact]
    public async Task RefusesAnAgentItDoesNotKnow()
    {
        Assert.Equal("agent.unknown", (await _crew.HireAsync("gemini", true, Token)).Error.Code);
        Assert.Contains("claude-code, codex", (await _crew.CheckAsync("gemini", Token)).Error.Message);
        Assert.Contains("knows no agents yet", new Crew([], new StaffFile(_folder)).Find("gemini").Error.Message);
    }

    [Fact]
    public async Task ChecksAnAgent()
    {
        var readiness = await new CheckAgentOp(_crew).ExecuteAsync(new CheckAgent("codex"), Token);

        Assert.Equal(new Blocked("Signed out", "Codex is not signed in.", "codex login"), readiness.Value);
    }

    [Fact]
    public async Task ReadsModesAndEffortsWhateverTheirCase()
    {
        await File.WriteAllTextAsync(
            AgentsFile,
            """{ "hired": ["claude-code"], "setups": { "claude-code": { "mode": "edits", "effort": "MAX" } } }""",
            Token);

        var card = (await _crew.CardsAsync(Token)).Value[0];

        Assert.True(card.Hired);
        Assert.Equal(new Setup(Mode.Edits, null, Effort.Max), card.Setup);
    }

    [Fact]
    public void WritesReadinessWithItsKind()
    {
        Assert.Contains("\"type\": \"blocked\"", JsonSerializer.Serialize<Readiness>(new Blocked("Signed out", "Not signed in."), BedrockJson.Options));
    }

    [Fact]
    public void ReadsTheModeAndEffortTheCommandLineTypes()
    {
        var input = BedrockJson.ReadInput(System.Text.Json.Nodes.JsonNode.Parse("""{ "agent": "claude-code", "mode": "plan", "effort": "max" }"""), typeof(ConfigureAgent));

        Assert.Equal(new ConfigureAgent("claude-code", Mode.Plan, null, Effort.Max), input.Value);
    }
}
