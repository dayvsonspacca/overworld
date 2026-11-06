using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Todos;

namespace Overworld.Apps.Crafter.Presentation.Aside;

public sealed partial class TodoViewModel : ObservableObject
{
    private readonly TodosViewModel _list;

    private bool _loading;

    public TodoViewModel(Todo todo, TodosViewModel list)
    {
        _list = list;
        Number = todo.Number;
        Text = todo.Text;
        _loading = true;
        Done = todo.Done;
        _loading = false;
    }

    public int Number { get; }

    public string Text { get; }

    public string Label => $"#{Number}";

    [ObservableProperty]
    public partial bool Done { get; set; }

    [RelayCommand]
    private Task RemoveAsync() => _list.RemoveAsync(this);

    partial void OnDoneChanged(bool value)
    {
        if (!_loading)
        {
            _ = _list.TickAsync(this, value);
        }
    }
}
