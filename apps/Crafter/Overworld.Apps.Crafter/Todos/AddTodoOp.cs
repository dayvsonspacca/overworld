using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Todos;

[Operation("add-todo", "Adds a todo to a project; it takes the number after the highest the project holds.")]
public sealed class AddTodoOp(TodoList list) : IOperation<AddTodo, Todo>
{
    public Task<Result<Todo>> ExecuteAsync(AddTodo input, CancellationToken cancellationToken)
    {
        var text = input.Text.Trim();

        if (text.Length == 0)
        {
            return Task.FromResult<Result<Todo>>(new Error("todo.empty", "A todo needs words."));
        }

        return list.ChangeAsync(
            input.Folder,
            todos =>
            {
                var added = new Todo(todos.Count > 0 ? todos.Max(todo => todo.Number) + 1 : 1, text);

                return Result.Success<(IReadOnlyList<Todo>, Todo)>(([.. todos, added], added));
            },
            cancellationToken);
    }
}
