using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Agents;
using Overworld.Allay.Jobs;

namespace Overworld.Apps.Crafter.Presentation.Jobs;

public sealed partial class JobViewModel(Job job, ISession session) : ObservableObject
{
    public string Id { get; } = job.Id;

    public string Description { get; } = job.Description;

    public string Kind { get; } = job.Kind switch
    {
        JobKind.Monitor => "Watching",
        JobKind.Agent => "Agent",
        _ => "Command",
    };

    public bool Prints { get; } = job.Prints;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutput))]
    public partial string? Output { get; private set; }

    [ObservableProperty]
    public partial bool IsOpen { get; private set; }

    public bool HasOutput => !string.IsNullOrEmpty(Output);

    [RelayCommand]
    private void Toggle()
    {
        IsOpen = !IsOpen;

        if (IsOpen)
        {
            Refresh();
        }
    }

    [RelayCommand]
    private void Refresh() => Output = session.Output(Id) ?? "Nothing printed yet.";

    [RelayCommand]
    private void Stop() => session.Stop(Id);
}
