using System.Collections.Concurrent;
using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Crafter.Todos;

public sealed class TodoList(Shelf shelf)
{
    private readonly ConcurrentDictionary<string, JsonFile<IReadOnlyList<Todo>>> _files = new();

    public async Task<Result<IReadOnlyList<Todo>>> ListAsync(string folder, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var todos = await FileOf(found.Value).ReadAsync(cancellationToken);

        return todos.IsSuccess ? Result.Success(Kept(todos.Value)) : todos.Error;
    }

    public async Task<Result<Todo>> ChangeAsync(
        string folder,
        Func<IReadOnlyList<Todo>, Result<(IReadOnlyList<Todo> Todos, Todo Changed)>> change,
        CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        Todo? changed = null;

        var written = await FileOf(found.Value).UpdateAsync(
            todos =>
            {
                var kept = Kept(todos);
                var result = change(kept);

                if (!result.IsSuccess)
                {
                    return result.Error;
                }

                changed = result.Value.Changed;

                return Result.Success(ReferenceEquals(result.Value.Todos, kept) ? todos : result.Value.Todos);
            },
            cancellationToken);

        return written.IsSuccess ? changed! : written.Error;
    }

    private static IReadOnlyList<Todo> Kept(IReadOnlyList<Todo> todos) =>
        [.. todos.Where(todo => todo.Number > 0 && !string.IsNullOrWhiteSpace(todo.Text)).DistinctBy(todo => todo.Number)];

    private JsonFile<IReadOnlyList<Todo>> FileOf(string folder) =>
        _files.GetOrAdd(folder, key => new JsonFile<IReadOnlyList<Todo>>(
            Path.Combine(key, ".crafter", "todos.json"),
            () => [],
            todos => todos.Count == 0));
}
