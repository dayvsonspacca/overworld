using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Todos;

[Operation("remove-todo", "Removes a project's todo, done or not.")]
public sealed class RemoveTodoOp(TodoList list) : IOperation<RemoveTodo, Todo>
{
    public Task<Result<Todo>> ExecuteAsync(RemoveTodo input, CancellationToken cancellationToken) =>
        list.ChangeAsync(
            input.Folder,
            todos => todos.FirstOrDefault(todo => todo.Number == input.Number) is { } found
                ? Result.Success<(IReadOnlyList<Todo>, Todo)>(([.. todos.Where(todo => todo != found)], found))
                : new Error("todo.missing", $"The project has no todo #{input.Number}."),
            cancellationToken);
}
