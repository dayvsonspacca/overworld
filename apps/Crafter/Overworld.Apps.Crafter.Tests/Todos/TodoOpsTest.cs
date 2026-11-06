using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Todos;

public sealed class TodoOpsTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly TodoList _list;
    private readonly string _overworld;

    public TodoOpsTest()
    {
        _shelf = new Shelf(_folder.PathTo("data"));
        _list = new TodoList(_shelf);
        _overworld = Directory.CreateDirectory(_folder.PathTo("overworld")).FullName;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string TodosFile => Path.Combine(_overworld, ".crafter", "todos.json");

    public void Dispose() => _folder.Dispose();

    private async Task OpenAsync() => await _shelf.AddAsync(_overworld, Token);

    private Task<Result<IReadOnlyList<Todo>>> ListAsync() =>
        new ListTodosOp(_list).ExecuteAsync(new ListTodos(_overworld), Token);

    private Task<Result<Todo>> AddAsync(string text) =>
        new AddTodoOp(_list).ExecuteAsync(new AddTodo(_overworld, text), Token);

    private Task<Result<Todo>> CompleteAsync(int number, bool done = true) =>
        new CompleteTodoOp(_list).ExecuteAsync(new CompleteTodo(_overworld, number, done), Token);

    private Task<Result<Todo>> RemoveAsync(int number) =>
        new RemoveTodoOp(_list).ExecuteAsync(new RemoveTodo(_overworld, number), Token);

    [Fact]
    public async Task ListsNoTodosBeforeAny()
    {
        await OpenAsync();

        Assert.Empty((await ListAsync()).Value);
    }

    [Fact]
    public async Task NumbersTodosInTheOrderTheyWereAdded()
    {
        await OpenAsync();

        await AddAsync("Bring the status bar");
        var added = await AddAsync("  Modal  ");

        Assert.Equal(new Todo(2, "Modal"), added.Value);
        Assert.Equal([new Todo(1, "Bring the status bar"), new Todo(2, "Modal")], (await ListAsync()).Value);
    }

    [Fact]
    public async Task NeverReusesTheHighestNumber()
    {
        await OpenAsync();
        await AddAsync("One");
        await AddAsync("Two");
        await RemoveAsync(1);

        var added = await AddAsync("Three");

        Assert.Equal(3, added.Value.Number);
    }

    [Fact]
    public async Task MarksATodoDoneAndNotDoneAgain()
    {
        await OpenAsync();
        await AddAsync("Modal");

        Assert.Equal(new Todo(1, "Modal", Done: true), (await CompleteAsync(1)).Value);
        Assert.Equal(new Todo(1, "Modal"), (await CompleteAsync(1, done: false)).Value);
        Assert.Equal([new Todo(1, "Modal")], (await ListAsync()).Value);
    }

    [Fact]
    public async Task LeavesTheFileAloneWhenATodoIsAlreadySo()
    {
        await OpenAsync();
        await AddAsync("Modal");
        var past = DateTime.UtcNow.AddHours(-1);
        File.SetLastWriteTimeUtc(TodosFile, past);

        await CompleteAsync(1, done: false);

        Assert.Equal(past, File.GetLastWriteTimeUtc(TodosFile));
    }

    [Fact]
    public async Task RemovesATodo()
    {
        await OpenAsync();
        await AddAsync("One");
        await AddAsync("Two");

        Assert.Equal(new Todo(1, "One"), (await RemoveAsync(1)).Value);
        Assert.Equal([new Todo(2, "Two")], (await ListAsync()).Value);
    }

    [Fact]
    public async Task TakesTheFileAwayWithTheLastTodo()
    {
        await OpenAsync();
        await AddAsync("One");

        await RemoveAsync(1);

        Assert.False(File.Exists(TodosFile));
    }

    [Fact]
    public async Task ReadsWhatCrafterWroteAndLeavesOutWhatItCouldNotKeep()
    {
        await OpenAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(TodosFile)!);
        await File.WriteAllTextAsync(
            TodosFile,
            """
            [
              { "number": 1, "text": "Modal", "done": true },
              { "number": 1, "text": "Again" },
              { "number": 0, "text": "Zero" },
              { "number": 2, "text": "  " },
              { "number": 3, "text": "Status bar", "done": false }
            ]
            """,
            Token);

        Assert.Equal([new Todo(1, "Modal", Done: true), new Todo(3, "Status bar")], (await ListAsync()).Value);
    }

    [Fact]
    public async Task WritesTheShapeCrafterWrote()
    {
        await OpenAsync();
        await AddAsync("Modal <dialog>");

        var json = await File.ReadAllTextAsync(TodosFile, Token);

        Assert.Contains("\"number\": 1", json);
        Assert.Contains("\"text\": \"Modal <dialog>\"", json);
        Assert.Contains("\"done\": false", json);
    }

    [Fact]
    public async Task RefusesABlankTodo()
    {
        await OpenAsync();

        Assert.Equal("todo.empty", (await AddAsync(" ")).Error.Code);
    }

    [Fact]
    public async Task RefusesATodoTheProjectDoesNotHave()
    {
        await OpenAsync();

        Assert.Equal("todo.missing", (await CompleteAsync(7)).Error.Code);
        Assert.Equal("todo.missing", (await RemoveAsync(7)).Error.Code);
    }

    [Fact]
    public async Task RefusesAProjectThatIsNotOpen()
    {
        Assert.Equal("project.not_open", (await ListAsync()).Error.Code);
        Assert.Equal("project.not_open", (await AddAsync("Modal")).Error.Code);
    }
}
