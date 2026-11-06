using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Todos;

[Operation("list-todos", "Lists a project's todos with their numbers, done or not, in the order they were added.")]
public sealed class ListTodosOp(TodoList list) : IOperation<ListTodos, IReadOnlyList<Todo>>
{
    public Task<Result<IReadOnlyList<Todo>>> ExecuteAsync(ListTodos input, CancellationToken cancellationToken) =>
        list.ListAsync(input.Folder, cancellationToken);
}
