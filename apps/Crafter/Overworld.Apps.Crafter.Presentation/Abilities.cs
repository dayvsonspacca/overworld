using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Pictures;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Crafter.Presentation;

public sealed class Abilities
{
    public required Operation<ListProjects, IReadOnlyList<Project>> ListProjects { get; init; }

    public required Operation<AddProject, Project> AddProject { get; init; }

    public required Operation<EditProject, Project> EditProject { get; init; }

    public required Operation<RemoveProject, Project> RemoveProject { get; init; }

    public required Operation<MoveProject, Project> MoveProject { get; init; }

    public required Operation<ListTodos, IReadOnlyList<Todo>> ListTodos { get; init; }

    public required Operation<AddTodo, Todo> AddTodo { get; init; }

    public required Operation<CompleteTodo, Todo> CompleteTodo { get; init; }

    public required Operation<RemoveTodo, Todo> RemoveTodo { get; init; }

    public required Operation<ListAxioms, IReadOnlyList<string>> ListAxioms { get; init; }

    public required Operation<AddAxiom, IReadOnlyList<string>> AddAxiom { get; init; }

    public required Operation<RemoveAxiom, IReadOnlyList<string>> RemoveAxiom { get; init; }

    public required Operation<ShowSummary, Summary> ShowSummary { get; init; }

    public required Operation<WriteSummary, Summary> WriteSummary { get; init; }

    public required Operation<ListProjectSkills, IReadOnlyList<Skill>> ListProjectSkills { get; init; }

    public required Operation<GiveSkill, IReadOnlyList<Skill>> GiveSkill { get; init; }

    public required Operation<ShowLibrary, LibraryState> ShowLibrary { get; init; }

    public required Operation<LinkLibrary, LibraryState> LinkLibrary { get; init; }

    public required Operation<SyncLibrary, LibraryState> SyncLibrary { get; init; }

    public required Operation<SetDefaultSkill, LibraryState> SetDefaultSkill { get; init; }

    public required Operation<ShowPreferences, UserPreferences> ShowPreferences { get; init; }

    public required Operation<SetProjectsRoot, UserPreferences> SetProjectsRoot { get; init; }

    public required ChatStore Chats { get; init; }

    public required PictureStore Pictures { get; init; }

    public required Workshop Workshop { get; init; }
}
