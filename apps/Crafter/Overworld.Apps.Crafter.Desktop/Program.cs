using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Desktop.Workbench;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Presentation;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Desktop;
using Overworld.Hosting;

var app = new AppIdentity("crafter");
var data = DataDirectory.For(app.Name);
var shelf = new Shelf(data);
var settings = new SettingsStore();
var todos = new TodoList(shelf);
var axioms = new AxiomBook(shelf, settings);
var library = new SkillLibrary(new PreferencesStore(data, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), new Mirror(Path.Combine(data, "library")));
var skills = new ProjectSkills(shelf, library);

var abilities = new Abilities(
    Operation.From(new ListProjectsOp(shelf, settings)),
    Operation.From(new ListTodosOp(todos)),
    Operation.From(new AddTodoOp(todos)),
    Operation.From(new CompleteTodoOp(todos)),
    Operation.From(new RemoveTodoOp(todos)),
    Operation.From(new ListAxiomsOp(axioms)),
    Operation.From(new AddAxiomOp(axioms)),
    Operation.From(new RemoveAxiomOp(axioms)),
    Operation.From(new ListProjectSkillsOp(skills)),
    new ChatStore(Path.Combine(data, "chats")));

return await AppHost.RunAsync(
    app.Command,
    [
        abilities.ListProjects,
        abilities.ListTodos,
        abilities.AddTodo,
        abilities.CompleteTodo,
        abilities.RemoveTodo,
        abilities.ListAxioms,
        abilities.AddAxiom,
        abilities.RemoveAxiom,
        abilities.ListProjectSkills,
    ],
    args,
    withoutArgs: () => Task.FromResult(DesktopHost.Run(app, frame =>
    {
        var workbench = new WorkbenchViewModel(abilities);
        var view = new WorkbenchView { DataContext = workbench };
        Shortcuts.Register(frame, workbench, view);
        _ = workbench.LoadAsync(CancellationToken.None);

        return view;
    }, args)));
