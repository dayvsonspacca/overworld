using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Todos;

namespace Overworld.Apps.Crafter.Presentation.Aside;

public sealed partial class TodosViewModel(string folder, Abilities abilities) : ObservableObject
{
    public ObservableCollection<TodoViewModel> Todos { get; } = [];

    public int Left => Todos.Count(todo => !todo.Done);

    public bool HasLeft => Left > 0;

    public bool HasTodos => Todos.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewTodo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var listed = await abilities.ListTodos.ExecuteAsync(new ListTodos(folder), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        Problem = null;
        Todos.Clear();

        foreach (var todo in listed.Value.OrderBy(todo => todo.Done))
        {
            Todos.Add(new TodoViewModel(todo, this));
        }

        Counted();
    }

    internal async Task TickAsync(TodoViewModel todo, bool done)
    {
        var ticked = await abilities.CompleteTodo.ExecuteAsync(new CompleteTodo(folder, todo.Number, done), CancellationToken.None);
        Problem = ticked.IsSuccess ? null : ticked.Error.Message;
        Counted();
    }

    internal async Task RemoveAsync(TodoViewModel todo)
    {
        var removed = await abilities.RemoveTodo.ExecuteAsync(new RemoveTodo(folder, todo.Number), CancellationToken.None);

        if (!removed.IsSuccess)
        {
            Problem = removed.Error.Message;

            return;
        }

        Todos.Remove(todo);
        Counted();
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var added = await abilities.AddTodo.ExecuteAsync(new AddTodo(folder, NewTodo), CancellationToken.None);

        if (!added.IsSuccess)
        {
            Problem = added.Error.Message;

            return;
        }

        Problem = null;
        NewTodo = string.Empty;
        Todos.Insert(Todos.Count(each => !each.Done), new TodoViewModel(added.Value, this));
        Counted();
    }

    private bool CanAdd() => NewTodo.Trim().Length > 0;

    private void Counted()
    {
        OnPropertyChanged(nameof(Left));
        OnPropertyChanged(nameof(HasLeft));
        OnPropertyChanged(nameof(HasTodos));
    }
}
