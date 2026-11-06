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
var library = new SkillLibrary(new PreferencesStore(data, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), new Mirror(Path.Combine(data, "library")));
var skills = new ProjectSkills(shelf, library);
var chats = new ChatStore(Path.Combine(data, "chats"));

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
        listTodos,
        addTodo,
        completeTodo,
        removeTodo,
        listAxioms,
        addAxiom,
        removeAxiom,
        listSkills,
    ],
    args,
    withoutArgs: () =>
    {
        using var tools = new McpHttpHost(app.Name);
        tools.Start();
        var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], new StaffFile(allay));
        var summaries = new SummaryStore();
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
