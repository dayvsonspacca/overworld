using Overworld.Apps.Crafter.Presentation.Aside;
using Overworld.Apps.Crafter.Todos;

namespace Overworld.Apps.Crafter.Presentation.Tests.Aside;

public sealed class AsideViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private async Task<(string Folder, AsideViewModel Aside)> LoadedAsync()
    {
        var folder = await _bench.ProjectAsync("alpha");
        var aside = new AsideViewModel(folder, _bench.Abilities);
        await aside.LoadAsync(Bench.Cancellation);

        return (folder, aside);
    }

    private async Task<IReadOnlyList<Todo>> SavedTodosAsync(string folder) =>
        (await _bench.Abilities.ListTodos.ExecuteAsync(new ListTodos(folder), Bench.Cancellation)).Value;

    [Fact]
    public async Task AddsATodoBeforeTheDoneOnes()
    {
        var (folder, aside) = await LoadedAsync();
        var todos = aside.Todos;
        todos.NewTodo = "First";
        await todos.AddCommand.ExecuteAsync(null);
        todos.Todos[0].Done = true;
        await Task.Delay(50, Bench.Cancellation);

        todos.NewTodo = "Second";
        await todos.AddCommand.ExecuteAsync(null);

        Assert.Equal(["Second", "First"], todos.Todos.Select(todo => todo.Text));
        Assert.Equal(1, todos.Left);
        Assert.Equal(string.Empty, todos.NewTodo);
        Assert.Contains(await SavedTodosAsync(folder), todo => todo is { Text: "First", Done: true });
    }

    [Fact]
    public async Task RemovesATodo()
    {
        var (folder, aside) = await LoadedAsync();
        aside.Todos.NewTodo = "Gone soon";
        await aside.Todos.AddCommand.ExecuteAsync(null);

        await aside.Todos.Todos[0].RemoveCommand.ExecuteAsync(null);

        Assert.Empty(aside.Todos.Todos);
        Assert.False(aside.Todos.HasTodos);
        Assert.Empty(await SavedTodosAsync(folder));
    }

    [Fact]
    public async Task RefusesABlankTodo()
    {
        var (_, aside) = await LoadedAsync();

        aside.Todos.NewTodo = "   ";

        Assert.False(aside.Todos.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddsAndRemovesAxiomsTellingEachChange()
    {
        var (_, aside) = await LoadedAsync();
        var changes = 0;
        aside.Axioms.Changed += (_, _) => changes++;

        aside.Axioms.NewAxiom = "Never add comments";
        await aside.Axioms.AddCommand.ExecuteAsync(null);
        Assert.Equal(["Never add comments"], aside.Axioms.Axioms);

        await aside.Axioms.RemoveCommand.ExecuteAsync("Never add comments");

        Assert.Empty(aside.Axioms.Axioms);
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task ShowsOneTabAtATime()
    {
        var (_, aside) = await LoadedAsync();

        aside.ShowSkillsCommand.Execute(null);

        Assert.Equal(AsideTab.Skills, aside.Tab);
        Assert.True(aside.ShowsSkills);
        Assert.False(aside.ShowsTodos);
        Assert.False(aside.Skills.HasSkills);
    }
}
