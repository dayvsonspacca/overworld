using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Bells;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Pictures;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Presentation.Tests.Fakes;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Operations;
using Overworld.Bell;
using Overworld.Mcp;
using Overworld.Redstone;

namespace Overworld.Apps.Crafter.Presentation.Tests;

public sealed class Bench : IDisposable
{
    public static readonly AgentId Scripted = new("scripted");

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
        Pictures = new PictureStore(PathTo("data", "pictures"));
        Chats = new ChatStore(PathTo("data", "chats"), Pictures);
        Agent = new ScriptedAgent(Session);
        Crew = new Crew([Agent], new StaffFile(PathTo("allay")));
        Summaries = new SummaryStore();
        Workshop = new Workshop(Crew, new ProjectBrief(Summaries, book, skills), new Toolbox(_host, [], [], book, Summaries), Chats);
        AddProject = Operation.From(new AddProjectOp(shelf, settings));
        var preferences = new PreferencesStore(data, _root);
        var library = new SkillLibrary(preferences, new Mirror(PathTo("data", "library")));
        Abilities = new Abilities
        {
            ListProjects = Operation.From(new ListProjectsOp(shelf, settings)),
            AddProject = AddProject,
            EditProject = Operation.From(new EditProjectOp(shelf, settings)),
            RemoveProject = Operation.From(new RemoveProjectOp(shelf, settings)),
            MoveProject = Operation.From(new MoveProjectOp(shelf, settings)),
            ListTodos = Operation.From(new ListTodosOp(todos)),
            AddTodo = Operation.From(new AddTodoOp(todos)),
            CompleteTodo = Operation.From(new CompleteTodoOp(todos)),
            RemoveTodo = Operation.From(new RemoveTodoOp(todos)),
            ListAxioms = Operation.From(new ListAxiomsOp(book)),
            AddAxiom = Operation.From(new AddAxiomOp(book)),
            RemoveAxiom = Operation.From(new RemoveAxiomOp(book)),
            ShowSummary = Operation.From(new ShowSummaryOp(shelf, Summaries)),
            WriteSummary = Operation.From(new WriteSummaryOp(shelf, Summaries)),
            ListProjectSkills = Operation.From(new ListProjectSkillsOp(skills)),
            GiveSkill = Operation.From(new GiveSkillOp(skills)),
            ShowLibrary = Operation.From(new ShowLibraryOp(library)),
            LinkLibrary = Operation.From(new LinkLibraryOp(library)),
            SyncLibrary = Operation.From(new SyncLibraryOp(library)),
            SetDefaultSkill = Operation.From(new SetDefaultSkillOp(library)),
            ShowPreferences = Operation.From(new ShowPreferencesOp(preferences)),
            SetProjectsRoot = Operation.From(new SetProjectsRootOp(preferences)),
            Chats = Chats,
            Pictures = Pictures,
            Lenders = [Lender],
            Workshop = Workshop,
            Ringer = new Ringer(new BellClient(new RedstoneClient(PathTo("overworld.sock")))),
            Attention = new Attention(),
        };
    }

    public ScriptedSession Session { get; } = new();

    public ScriptedAgent Agent { get; }

    public Crew Crew { get; }

    public SummaryStore Summaries { get; }

    public Workshop Workshop { get; }

    public Abilities Abilities { get; }

    public ChatStore Chats { get; }

    public PictureStore Pictures { get; }

    public ScriptedLender Lender { get; } = new();

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

    public void Skill(string id, string description)
    {
        Directory.CreateDirectory(PathTo("data"));
        File.WriteAllText(PathTo("data", "preferences.json"), """{ "library": "git@example.org:skills.git" }""");
        var place = Directory.CreateDirectory(PathTo("data", "library", "skills", id)).FullName;
        File.WriteAllText(Path.Combine(place, "skill.json"), $$"""{ "name": "{{id}}", "description": "{{description}}" }""");
        File.WriteAllText(Path.Combine(place, "instructions.md"), "Do it well.");
    }

    public async Task HireAsync() => Assert.True((await Crew.HireAsync(Scripted.Value, true, Cancellation)).IsSuccess);

    public async Task ChatsAsync(string folder, params ChatRecord[] chats) =>
        Assert.True((await Chats.SaveAsync(folder, chats, Cancellation)).IsSuccess);

    public ChatSummaryViewModel Summary(ChatRecord record) => new(record, PathTo("projects"), Abilities);

    public static ChatRecord Chat(string title, params Turn[] turns) =>
        new(ChatId.New(), Scripted, ChatTitle.Of(title), null, turns);

    public void Dispose()
    {
        Workshop.CloseAll();
        _host.Dispose();
        Directory.Delete(_root, recursive: true);
    }
}
