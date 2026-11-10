using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public abstract partial class ProposalEntry : Entry
{
    private readonly TaskCompletionSource<bool> _decision = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<bool> Decision => _decision.Task;

    public bool IsOpen => Verdict is null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen))]
    public partial string? Verdict { get; private set; }

    public void Withdraw()
    {
        if (IsOpen)
        {
            Verdict = "Withdrawn";
            _decision.TrySetResult(false);
        }
    }

    [RelayCommand]
    private void Approve() => Decide(true, "Approved · the project keeps it now");

    [RelayCommand]
    private void Decline() => Decide(false, "Declined");

    private void Decide(bool approved, string verdict)
    {
        if (IsOpen)
        {
            Verdict = verdict;
            _decision.TrySetResult(approved);
        }
    }
}
