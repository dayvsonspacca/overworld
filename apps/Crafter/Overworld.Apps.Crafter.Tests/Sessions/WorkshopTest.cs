using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Tests.Fakes;
using Overworld.Apps.Crafter.Todos;
using Overworld.Mcp;

namespace Overworld.Apps.Crafter.Tests.Sessions;

public sealed class WorkshopTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly McpHttpHost _host = new("crafter");

    private readonly ScriptedSession _session = new([new Speech("Olá"), new Finished()]);

    private readonly ScriptedAgent _agent;

    private readonly Crew _crew;

    private readonly AxiomBook _axioms;

    private readonly ChatStore _store;

    private readonly SummaryStore _summaries = new();

    private readonly Workshop _workshop;

    private readonly string _project;

    public WorkshopTest()
    {
        _host.Start();
        var shelf = new Shelf(_folder.PathTo("data"));
        _project = Directory.CreateDirectory(_folder.PathTo("project")).FullName;
        _agent = new ScriptedAgent("scripted", _session);
        _crew = new Crew([_agent], new StaffFile(_folder.PathTo("allay")));
        _axioms = new AxiomBook(shelf, new SettingsStore());
        var skills = new ProjectSkills(shelf, new SkillLibrary(new PreferencesStore(_folder.PathTo("data"), "/home/someone"), new Mirror(_folder.PathTo("data", "library"))));
        _store = new ChatStore(_folder.PathTo("chats"));
        _workshop = new Workshop(_crew, new ProjectBrief(_summaries, _axioms, skills), new Toolbox(_host, [], [], _axioms, _summaries), _store);
        shelf.AddAsync(_project, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _workshop.CloseAll();
        _host.Dispose();
        _folder.Dispose();
    }

    [Fact]
    public async Task RefusesAFreshChatWhileNoAgentIsHired()
    {
        var fresh = await _workshop.FreshAsync(Token);

        Assert.Equal("crafter.no_agent", fresh.Error.Code);
    }

    [Fact]
    public async Task GivesAFreshChatToTheFirstAgentHired()
    {
        await _crew.HireAsync("scripted", true, Token);

        var fresh = (await _workshop.FreshAsync(Token)).Value;

        Assert.Equal(new AgentId("scripted"), fresh.Agent);
        Assert.Equal(ChatTitle.Untitled, fresh.Title);
        Assert.Empty(fresh.Turns);
    }

    [Fact]
    public async Task TellsTheDeskOfAChatsAgent()
    {
        await _crew.SetUpAsync("scripted", Mode.Plan, null, Effort.Max, Token);

        var desk = (await _workshop.DeskAsync(new ChatRecord(ChatId.New(), new AgentId("scripted"), ChatTitle.Untitled, null, []), Token)).Value;

        Assert.Equal(new Desk("Scripted", new Setup(Mode.Plan, null, Effort.Max), [new ModelOption("fast", "Fast")]), desk);
    }

    [Fact]
    public async Task OpensAChatOnceBriefedWithTheProjectAndItsTools()
    {
        await _axioms.ChangeAsync(_project, _ => Bedrock.Results.Result.Success<IReadOnlyList<string>>(["Never add comments."]), Token);
        var record = new ChatRecord(ChatId.New(), new AgentId("scripted"), ChatTitle.Of("Old"), "s-1", [new Prompt("oi", [], [])]);

        var chat = (await _workshop.OpenAsync(_project, record, Token)).Value;
        var again = (await _workshop.OpenAsync(_project, record, Token)).Value;

        Assert.Same(chat, again);
        var (brief, resume) = Assert.Single(_agent.Begun);
        Assert.Equal("s-1", resume);
        Assert.Equal(["Never add comments."], brief.Rules);
        Assert.Equal(Toolbox.Name, Assert.Single(brief.Tools).Name);
        Assert.Single(chat.Turns);
    }

    [Fact]
    public async Task KeepsWhatAChatSaidInTheStore()
    {
        var record = new ChatRecord(ChatId.New(), new AgentId("scripted"), ChatTitle.Untitled, null, []);
        var chat = (await _workshop.OpenAsync(_project, record, Token)).Value;
        await foreach (var _ in chat.AskAsync(new Prompt("oi", [], []), Setup.Default, Token))
        {
        }

        await _workshop.KeepAsync(_project, chat, Token);
        await _workshop.KeepAsync(_project, chat, Token);

        var kept = Assert.Single((await _store.LoadAsync(_project, Token)).Value);
        Assert.Equal("oi", kept.Title.Text);
        Assert.Equal(2, kept.Turns.Count);
    }

    [Fact]
    public async Task RebriefsTheOpenChatsOfAProject()
    {
        var record = new ChatRecord(ChatId.New(), new AgentId("scripted"), ChatTitle.Untitled, null, []);
        await _workshop.OpenAsync(_project, record, Token);
        await _axioms.ChangeAsync(_project, _ => Bedrock.Results.Result.Success<IReadOnlyList<string>>(["Be brief."]), Token);

        await _workshop.RebriefAsync(_project, Token);

        Assert.Equal(["Be brief."], Assert.Single(_session.Briefs).Rules);
    }

    [Fact]
    public async Task EndsTheSessionWhenAChatCloses()
    {
        var record = new ChatRecord(ChatId.New(), new AgentId("scripted"), ChatTitle.Untitled, null, []);
        var chat = (await _workshop.OpenAsync(_project, record, Token)).Value;
        var ended = false;
        chat.Session.Woke += (_, _) => ended = true;

        _workshop.Close(record.Id);
        var reopened = (await _workshop.OpenAsync(_project, record, Token)).Value;

        Assert.True(ended);
        Assert.NotSame(chat, reopened);
    }
}
