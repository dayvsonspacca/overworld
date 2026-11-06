using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Desktop.Workbench;
using Overworld.Apps.Crafter.Imports;
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

var listProjects = Operation.From(new ListProjectsOp(shelf, settings));
var listTodos = Operation.From(new ListTodosOp(todos));
var addTodo = Operation.From(new AddTodoOp(todos));
var completeTodo = Operation.From(new CompleteTodoOp(todos));
var removeTodo = Operation.From(new RemoveTodoOp(todos));
var listAxioms = Operation.From(new ListAxiomsOp(axioms));
var addAxiom = Operation.From(new AddAxiomOp(axioms));
var removeAxiom = Operation.From(new RemoveAxiomOp(axioms));
var listSkills = Operation.From(new ListProjectSkillsOp(skills));

return await AppHost.RunAsync(
    app.Command,
    [
        listProjects,
        Operation.From(new AddProjectOp(shelf, settings)),
        Operation.From(new EditProjectOp(shelf, settings)),
        Operation.From(new RemoveProjectOp(shelf, settings)),
        listAxioms,
        addAxiom,
        removeAxiom,
        Operation.From(new ShowSummaryOp(shelf, summaries)),
        Operation.From(new WriteSummaryOp(shelf, summaries)),
        listTodos,
        addTodo,
        completeTodo,
        removeTodo,
        Operation.From(new ShowPreferencesOp(preferences)),
        Operation.From(new SetProjectsRootOp(preferences)),
        Operation.From(new ShowLibraryOp(library)),
        Operation.From(new LinkLibraryOp(library)),
        Operation.From(new SyncLibraryOp(library)),
        Operation.From(new SaveSkillOp(library)),
        Operation.From(new SetDefaultSkillOp(library)),
        listSkills,
        Operation.From(new GiveSkillOp(skills)),
        Operation.From(new ImportOldCrafterOp(OldCrafter.Here(), data, staff)),
    ],
    args,
    withoutArgs: () =>
    {
        using var tools = new McpHttpHost(app.Name);
        tools.Start();
        var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], staff);
        var workshop = new Workshop(crew, new ProjectBrief(summaries, axioms, skills), new Toolbox(tools, todos, axioms, summaries), chats);
        var abilities = new Abilities(listProjects, listTodos, addTodo, completeTodo, removeTodo, listAxioms, addAxiom, removeAxiom, listSkills, chats, workshop);

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
