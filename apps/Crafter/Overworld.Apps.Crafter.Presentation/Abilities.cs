using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Crafter.Presentation;

public sealed record Abilities(
    Operation<ListProjects, IReadOnlyList<Project>> ListProjects,
    Operation<ListTodos, IReadOnlyList<Todo>> ListTodos,
    Operation<AddTodo, Todo> AddTodo,
    Operation<CompleteTodo, Todo> CompleteTodo,
    Operation<RemoveTodo, Todo> RemoveTodo,
    Operation<ListAxioms, IReadOnlyList<string>> ListAxioms,
    Operation<AddAxiom, IReadOnlyList<string>> AddAxiom,
    Operation<RemoveAxiom, IReadOnlyList<string>> RemoveAxiom,
    Operation<ListProjectSkills, IReadOnlyList<Skill>> ListProjectSkills,
    ChatStore Chats);
