using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public abstract partial class RequestEntry(string id, Action<string, Decision> decide) : Entry
{
    public string Id { get; } = id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen))]
    public partial string? Answer { get; private set; }

    public bool IsOpen => Answer is null;

    public void Withdraw() => Answer ??= "Withdrawn";

    protected void Decide(Decision decision, string answer)
    {
        if (!IsOpen)
        {
            return;
        }

        Answer = answer;
        decide(Id, decision);
    }
}
