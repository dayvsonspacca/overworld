using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Todos;

[Operation("complete-todo", "Marks a project's todo done, or not done again.")]
public sealed class CompleteTodoOp(TodoList list) : IOperation<CompleteTodo, Todo>
{
    public Task<Result<Todo>> ExecuteAsync(CompleteTodo input, CancellationToken cancellationToken) =>
        list.ChangeAsync(
            input.Folder,
            todos =>
            {
                if (todos.FirstOrDefault(todo => todo.Number == input.Number) is not { } found)
                {
                    return new Error("todo.missing", $"The project has no todo #{input.Number}.");
                }

                var ticked = found with { Done = input.Done };

                return Result.Success<(IReadOnlyList<Todo>, Todo)>(
                    (found == ticked ? todos : [.. todos.Select(todo => todo == found ? ticked : todo)], ticked));
            },
            cancellationToken);
}
