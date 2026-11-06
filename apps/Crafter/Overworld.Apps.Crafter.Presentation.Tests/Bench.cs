using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Presentation.Tests.Fakes;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Operations;
using Overworld.Mcp;

namespace Overworld.Apps.Crafter.Presentation.Tests;

public sealed class Bench : IDisposable
{
    public static readonly AgentId Agent = new("scripted");

    private readonly string _root = Directory.CreateTempSubdirectory("crafter-presentation-").FullName;

    private readonly McpHttpHost _host = new("crafter");

    public Bench()
    {
        _host.Start();
        var data = PathTo("data");
        var shelf = new Shelf(data);
        var settings = new SettingsStore();
        var todos = new TodoList(shelf);
        var book = new AxiomBook(shelf, settings);
        var skills = new ProjectSkills(shelf, new SkillLibrary(new PreferencesStore(data, _root), new Mirror(PathTo("data", "library"))));
        Chats = new ChatStore(PathTo("data", "chats"));
        Crew = new Crew([new ScriptedAgent(Session)], new StaffFile(PathTo("allay")));
        Workshop = new Workshop(Crew, new ProjectBrief(new SummaryStore(), book, skills), new Toolbox(_host, todos), Chats);
        AddProject = Operation.From(new AddProjectOp(shelf, settings));
        Abilities = new Abilities(
            Operation.From(new ListProjectsOp(shelf, settings)),
            Operation.From(new ListTodosOp(todos)),
            Operation.From(new AddTodoOp(todos)),
            Operation.From(new CompleteTodoOp(todos)),
            Operation.From(new RemoveTodoOp(todos)),
            Operation.From(new ListAxiomsOp(book)),
            Operation.From(new AddAxiomOp(book)),
            Operation.From(new RemoveAxiomOp(book)),
            Operation.From(new ListProjectSkillsOp(skills)),
            Chats,
            Workshop);
    }

    public ScriptedSession Session { get; } = new();

    public Crew Crew { get; }

    public Workshop Workshop { get; }

    public Abilities Abilities { get; }

    public ChatStore Chats { get; }

    public Operation<AddProject, Project> AddProject { get; }

    public static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public string PathTo(params string[] parts) => Path.Combine([_root, .. parts]);

    public async Task<string> ProjectAsync(string name)
    {
        var folder = Directory.CreateDirectory(PathTo("projects", name)).FullName;
        var added = await AddProject.ExecuteAsync(new AddProject(folder), Cancellation);

        Assert.True(added.IsSuccess);

        return folder;
    }

    public async Task HireAsync() => Assert.True((await Crew.HireAsync(Agent.Value, true, Cancellation)).IsSuccess);

    public async Task ChatsAsync(string folder, params ChatRecord[] chats) =>
        Assert.True((await Chats.SaveAsync(folder, chats, Cancellation)).IsSuccess);

    public ChatSummaryViewModel Summary(ChatRecord record) => new(record, PathTo("projects"), Workshop);

    public static ChatRecord Chat(string title, params Turn[] turns) =>
        new(ChatId.New(), Agent, ChatTitle.Of(title), null, turns);

    public void Dispose()
    {
        Workshop.CloseAll();
        _host.Dispose();
        Directory.Delete(_root, recursive: true);
    }
}
