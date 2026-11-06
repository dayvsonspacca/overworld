using System.Text.Json;
using Overworld.Allay.Agents;
using Overworld.Allay.Hiring;
using Overworld.Allay.Tests.Fakes;
using Overworld.Bedrock.Json;

namespace Overworld.Allay.Tests.Hiring;

public sealed class CrewTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("allay-").FullName;
    private readonly ScriptedAgent _claude = new("claude-code");
    private readonly ScriptedAgent _codex = new("codex", new Blocked("Signed out", "Codex is not signed in.", "codex login"));
    private readonly Crew _crew;

    public CrewTest() => _crew = new Crew([_claude, _codex], new StaffFile(_folder));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string AgentsFile => Path.Combine(_folder, "agents.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task ListsEveryAgentUnhiredAtFirst()
    {
        var cards = (await new ListAgentsOp(_crew).ExecuteAsync(new ListAgents(), Token)).Value;

        Assert.Equal(["claude-code", "codex"], cards.Select(card => card.Id));
        Assert.All(cards, card => Assert.False(card.Hired));
        Assert.Equal(["model", "executable"], cards[0].Settings.Select(setting => setting.Key));
        Assert.False(File.Exists(AgentsFile));
    }

    [Fact]
    public async Task HiresAnAgentAndLetsItGo()
    {
        var hired = await new HireAgentOp(_crew).ExecuteAsync(new HireAgent("claude-code"), Token);

        Assert.True(hired.Value.Hired);
        Assert.Equal([_claude], (await _crew.HiredAsync(Token)).Value);

        var gone = await new HireAgentOp(_crew).ExecuteAsync(new HireAgent("claude-code", Hired: false), Token);

        Assert.False(gone.Value.Hired);
        Assert.Empty((await _crew.HiredAsync(Token)).Value);
    }

    [Fact]
    public async Task ConfiguresAnAgentForEveryApp()
    {
        var configured = await new ConfigureAgentOp(_crew).ExecuteAsync(new ConfigureAgent("claude-code", "model", "big"), Token);

        Assert.Equal("big", configured.Value.Configuration["model"]);
        Assert.Equal("big", (await new Crew([_claude], new StaffFile(_folder)).ConfigurationOfAsync("claude-code", Token)).Value["model"]);
    }

    [Fact]
    public async Task PutsASettingBackToItsDefault()
    {
        await _crew.ConfigureAsync("claude-code", "model", "big", Token);

        var configured = await _crew.ConfigureAsync("claude-code", "model", null, Token);

        Assert.Empty(configured.Value.Configuration);
        Assert.DoesNotContain("configurations", await File.ReadAllTextAsync(AgentsFile, Token));
    }

    [Fact]
    public async Task TakesAnyValueForAnEntry()
    {
        var configured = await _crew.ConfigureAsync("claude-code", "executable", "/opt/claude", Token);

        Assert.Equal("/opt/claude", configured.Value.Configuration["executable"]);
    }

    [Fact]
    public async Task RefusesAValueAChoiceDoesNotOffer()
    {
        var configured = await _crew.ConfigureAsync("claude-code", "model", "huge", Token);

        Assert.Equal("agent.value_unknown", configured.Error.Code);
        Assert.Contains("(default), big, small", configured.Error.Message);
    }

    [Fact]
    public async Task RefusesASettingTheAgentLacks()
    {
        Assert.Equal("agent.setting_unknown", (await _crew.ConfigureAsync("claude-code", "colour", "red", Token)).Error.Code);
    }

    [Fact]
    public async Task RefusesAnAgentItDoesNotKnow()
    {
        Assert.Equal("agent.unknown", (await _crew.HireAsync("gemini", true, Token)).Error.Code);
        Assert.Contains("claude-code, codex", (await _crew.CheckAsync("gemini", Token)).Error.Message);
        Assert.Contains("knows no agents yet", new Crew([], new StaffFile(_folder)).Find("gemini").Error.Message);
    }

    [Fact]
    public async Task ChecksAnAgentWithItsConfiguration()
    {
        await _crew.ConfigureAsync("codex", "model", "small", Token);

        var readiness = await new CheckAgentOp(_crew).ExecuteAsync(new CheckAgent("codex"), Token);

        Assert.Equal(new Blocked("Signed out", "Codex is not signed in.", "codex login"), readiness.Value);
        Assert.Equal("small", _codex.CheckedWith!["model"]);
    }

    [Fact]
    public async Task ReadsWhoIsHiredAndHowEachIsSet()
    {
        await File.WriteAllTextAsync(
            AgentsFile,
            """{ "hired": ["claude-code"], "configurations": { "claude-code": { "tools": "auto" } } }""",
            Token);

        var card = (await _crew.CardsAsync(Token)).Value[0];

        Assert.True(card.Hired);
        Assert.Equal("auto", card.Configuration["tools"]);
    }

    [Fact]
    public void WritesCardsAndReadinessWithTheirKinds()
    {
        var json = JsonSerializer.Serialize<Readiness>(new Blocked("Signed out", "Not signed in."), BedrockJson.Options);
        var card = JsonSerializer.Serialize(
            new AgentCard("claude-code", "Claude Code", "Agent", true, _claude.Settings(Overworld.Allay.Settings.Configuration.Empty), new Dictionary<string, string>()),
            BedrockJson.Options);

        Assert.Contains("\"type\": \"blocked\"", json);
        Assert.Contains("\"type\": \"choice\"", card);
        Assert.Contains("\"type\": \"entry\"", card);
    }
}
