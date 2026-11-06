using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Desktop.Workbench;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Presentation;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Desktop;
using Overworld.Hosting;
using Overworld.Mcp;

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
var chats = new ChatStore(Path.Combine(data, "chats"));
var staff = new StaffFile(allay);
using var tools = new McpHttpHost(app.Name);
var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], staff);
var workshop = new Workshop(crew, new ProjectBrief(summaries, axioms, skills), new Toolbox(tools, todos, axioms, summaries), chats);

var abilities = new Abilities
{
    ListProjects = Operation.From(new ListProjectsOp(shelf, settings)),
    AddProject = Operation.From(new AddProjectOp(shelf, settings)),
    EditProject = Operation.From(new EditProjectOp(shelf, settings)),
    RemoveProject = Operation.From(new RemoveProjectOp(shelf, settings)),
    ListTodos = Operation.From(new ListTodosOp(todos)),
    AddTodo = Operation.From(new AddTodoOp(todos)),
    CompleteTodo = Operation.From(new CompleteTodoOp(todos)),
    RemoveTodo = Operation.From(new RemoveTodoOp(todos)),
    ListAxioms = Operation.From(new ListAxiomsOp(axioms)),
    AddAxiom = Operation.From(new AddAxiomOp(axioms)),
    RemoveAxiom = Operation.From(new RemoveAxiomOp(axioms)),
    ShowSummary = Operation.From(new ShowSummaryOp(shelf, summaries)),
    WriteSummary = Operation.From(new WriteSummaryOp(shelf, summaries)),
    ListProjectSkills = Operation.From(new ListProjectSkillsOp(skills)),
    GiveSkill = Operation.From(new GiveSkillOp(skills)),
    ShowLibrary = Operation.From(new ShowLibraryOp(library)),
    LinkLibrary = Operation.From(new LinkLibraryOp(library)),
    SyncLibrary = Operation.From(new SyncLibraryOp(library)),
    SetDefaultSkill = Operation.From(new SetDefaultSkillOp(library)),
    ShowPreferences = Operation.From(new ShowPreferencesOp(preferences)),
    SetProjectsRoot = Operation.From(new SetProjectsRootOp(preferences)),
    Chats = chats,
    Workshop = workshop,
};

return await AppHost.RunAsync(
    app.Command,
    [
        abilities.ListProjects,
        abilities.AddProject,
        abilities.EditProject,
        abilities.RemoveProject,
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
        Operation.From(new SaveSkillOp(library)),
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

            return view;
        }, args);

        workshop.CloseAll();

        return Task.FromResult(code);
    });
