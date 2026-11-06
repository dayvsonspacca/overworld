using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Crafter.Presentation.Tests;

public sealed class Bench : IDisposable
{
    private static readonly AgentId Claude = new("claude-code");

    private readonly string _root = Directory.CreateTempSubdirectory("crafter-presentation-").FullName;

    public Bench()
    {
        var data = PathTo("data");
        var shelf = new Shelf(data);
        var settings = new SettingsStore();
        var todos = new TodoList(shelf);
        var book = new AxiomBook(shelf, settings);
        var skills = new ProjectSkills(shelf, new SkillLibrary(new PreferencesStore(data, _root), new Mirror(PathTo("data", "library"))));
        Chats = new ChatStore(PathTo("data", "chats"));
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
            Chats);
    }

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

    public async Task ChatsAsync(string folder, params ChatRecord[] chats) =>
        Assert.True((await Chats.SaveAsync(folder, chats, Cancellation)).IsSuccess);

    public static ChatRecord Chat(string title, params Turn[] turns) =>
        new(ChatId.New(), Claude, ChatTitle.Of(title), null, turns);

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
