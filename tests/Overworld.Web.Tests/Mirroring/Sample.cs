using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Overworld.Web.Tests.Mirroring;

public sealed record Swatch(string Name, string Hex);

public sealed partial class Sample : ObservableObject
{
    public ObservableCollection<Swatch> Swatches { get; } = [new("sea", "#003F5C")];

    public Child Child { get; } = new();

    public Task Pending { get; } = new TaskCompletionSource().Task;

    public string Secret => Name;

    [ObservableProperty]
    public partial string Name { get; set; } = "first";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShoutCommand))]
    public partial bool IsLoud { get; set; }

    [ObservableProperty]
    public partial Swatch? Picked { get; set; }

    public List<string> Heard { get; } = [];

    public int Add(int first, int second) => first + second + Heard.Count;

    public async Task<string> EchoAsync(string text, CancellationToken cancellationToken)
    {
        await Task.Yield();

        return Name.Length > 0 ? text : string.Empty;
    }

    [RelayCommand]
    private void Say(string text) => Heard.Add(text);

    [RelayCommand(CanExecute = nameof(IsLoud))]
    private void Shout() => Heard.Add("!");
}

public sealed partial class Child : ObservableObject
{
    [ObservableProperty]
    public partial int Count { get; set; }
}
