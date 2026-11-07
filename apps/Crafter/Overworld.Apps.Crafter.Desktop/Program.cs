using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Bells;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Desktop.Gate;
using Overworld.Apps.Crafter.Desktop.Workbench;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Pictures;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Presentation;
using Overworld.Apps.Crafter.Presentation.Gate;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Beacon;
using Overworld.Bedrock.Apps;
using Overworld.Bell;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Desktop;
using Overworld.Hosting;
using Overworld.Mcp;
using Overworld.Redstone;

var app = new AppIdentity("crafter");
var data = DataDirectory.For(app.Name);
var allay = DataDirectory.For("allay");
var shelf = new Shelf(data);
var settings = new SettingsStore();
var todos = new TodoList(shelf);
var axioms = new AxiomBook(shelf, settings);
var summaries = new SummaryStore();
var preferences = new PreferencesStore(data, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
var library = new SkillLibrary(preferences, new Mirror(Path.Combine(data, "library")));
var skills = new ProjectSkills(shelf, library);
var pictures = new PictureStore(Path.Combine(data, "pictures"));
var chats = new ChatStore(Path.Combine(data, "chats"), pictures);
var staff = new StaffFile(allay);
using var tools = new McpHttpHost(app.Name);
var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], staff);
var editProject = Operation.From(new EditProjectOp(shelf, settings));
var listTodos = Operation.From(new ListTodosOp(todos));
var addTodo = Operation.From(new AddTodoOp(todos));
var completeTodo = Operation.From(new CompleteTodoOp(todos));
var removeTodo = Operation.From(new RemoveTodoOp(todos));
var giveSkill = Operation.From(new GiveSkillOp(skills));
var showLibrary = Operation.From(new ShowLibraryOp(library));
var saveSkill = Operation.From(new SaveSkillOp(library));
var toolbox = new Toolbox(
    tools,
    [listTodos, showLibrary, Operation.From(new ListChatsOp(chats)), Operation.From(new ReadChatOp(chats)), Operation.From(new ShowPictureOp(pictures))],
    [addTodo, completeTodo, removeTodo, editProject, giveSkill, saveSkill],
    axioms,
    summaries);
var workshop = new Workshop(crew, new ProjectBrief(summaries, axioms, skills), toolbox, chats);
var ringer = new Ringer(new BellClient(new RedstoneClient(SocketPath.Resolve())));
var attention = new Attention();

var abilities = new Abilities
{
    ListProjects = Operation.From(new ListProjectsOp(shelf, settings)),
    AddProject = Operation.From(new AddProjectOp(shelf, settings)),
    EditProject = editProject,
    RemoveProject = Operation.From(new RemoveProjectOp(shelf, settings)),
    MoveProject = Operation.From(new MoveProjectOp(shelf, settings)),
    ListTodos = listTodos,
    AddTodo = addTodo,
    CompleteTodo = completeTodo,
    RemoveTodo = removeTodo,
    ListAxioms = Operation.From(new ListAxiomsOp(axioms)),
    AddAxiom = Operation.From(new AddAxiomOp(axioms)),
    RemoveAxiom = Operation.From(new RemoveAxiomOp(axioms)),
    ShowSummary = Operation.From(new ShowSummaryOp(shelf, summaries)),
    WriteSummary = Operation.From(new WriteSummaryOp(shelf, summaries)),
    ListProjectSkills = Operation.From(new ListProjectSkillsOp(skills)),
    GiveSkill = giveSkill,
    ShowLibrary = showLibrary,
    LinkLibrary = Operation.From(new LinkLibraryOp(library)),
    SyncLibrary = Operation.From(new SyncLibraryOp(library)),
    SetDefaultSkill = Operation.From(new SetDefaultSkillOp(library)),
    ShowPreferences = Operation.From(new ShowPreferencesOp(preferences)),
    SetProjectsRoot = Operation.From(new SetProjectsRootOp(preferences)),
    Chats = chats,
    Pictures = pictures,
    Workshop = workshop,
    Ringer = ringer,
    Attention = attention,
};

return await AppHost.RunAsync(
    app.Command,
    [
        abilities.ListProjects,
        abilities.AddProject,
        abilities.EditProject,
        abilities.RemoveProject,
        abilities.MoveProject,
        abilities.ListAxioms,
        abilities.AddAxiom,
        abilities.RemoveAxiom,
        abilities.ShowSummary,
        abilities.WriteSummary,
        abilities.ListTodos,
        abilities.AddTodo,
        abilities.CompleteTodo,
        abilities.RemoveTodo,
        abilities.ShowPreferences,
        abilities.SetProjectsRoot,
        abilities.ShowLibrary,
        abilities.LinkLibrary,
        abilities.SyncLibrary,
        saveSkill,
        abilities.SetDefaultSkill,
        abilities.ListProjectSkills,
        abilities.GiveSkill,
    ],
    args,
    withoutArgs: () =>
    {
        tools.Start();

        var code = DesktopHost.Run(app, frame =>
        {
            var workbench = new WorkbenchViewModel(abilities);
            var view = new WorkbenchView { DataContext = workbench };
            Shortcuts.Register(frame, workbench, view);
            _ = workbench.LoadAsync(CancellationToken.None);

            var gate = new GateViewModel(Operation.From(new ListAgentsOp(crew)), Operation.From(new CheckAgentOp(crew)), OpenAllay);
            view.Bind(InputElement.IsEnabledProperty, new Binding(nameof(GateViewModel.IsShut)) { Source = gate, Converter = BoolConverters.Not });
            _ = gate.LoadAsync(CancellationToken.None);
            _ = gate.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);
            _ = ringer.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);

            var shown = new Panel { Children = { view, new GateView { DataContext = gate } } };
            shown.AttachedToVisualTree += (_, _) =>
            {
                if (TopLevel.GetTopLevel(shown) is Window window)
                {
                    attention.IsActive = window.IsActive;
                    window.Activated += (_, _) => attention.IsActive = true;
                    window.Deactivated += (_, _) => attention.IsActive = false;
                    attention.Summoned += (_, _) => window.Activate();
                }
            };

            return shown;
        }, args);

        ringer.WithdrawAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        workshop.CloseAll();

        return Task.FromResult(code);
    });

static bool OpenAllay()
{
    try
    {
        using var allay = Process.Start(new ProcessStartInfo("overworld:allay") { UseShellExecute = false });

        return allay is not null;
    }
    catch (Win32Exception)
    {
        return false;
    }
}
