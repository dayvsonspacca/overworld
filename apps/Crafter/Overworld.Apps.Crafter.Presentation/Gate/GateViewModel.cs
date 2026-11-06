using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Agents;
using Overworld.Allay.Hiring;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Crafter.Presentation.Gate;

public sealed partial class GateViewModel(
    Operation<ListAgents, IReadOnlyList<AgentCard>> list,
    Operation<CheckAgent, Readiness> check,
    Func<bool> openAllay) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsShut { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Heading), nameof(Message), nameof(Failed))]
    public partial bool NobodyHired { get; private set; }

    [ObservableProperty]
    public partial string? Agent { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Failed))]
    public partial string? Trouble { get; private set; }

    public bool Failed => NobodyHired && !string.IsNullOrEmpty(Trouble);

    [ObservableProperty]
    public partial bool IsChecking { get; private set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    public string Heading => NobodyHired ? "Crafter needs an agent" : "Your agent can't work yet";

    public string Message => NobodyHired
        ? "Hire one in Allay to start chatting. Crafter opens on its own once an agent is hired."
        : "Crafter's hired agents aren't ready on this machine. Fix it in Allay, or hire another one.";

    public async Task FollowAsync(IAsyncEnumerable<string> signals, CancellationToken cancellationToken)
    {
        await foreach (var signal in signals.WithCancellation(cancellationToken))
        {
            if (signal == Crew.Signal)
            {
                await LoadAsync(cancellationToken);
            }
        }
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsChecking = true;

        try
        {
            var listed = await list.ExecuteAsync(new ListAgents(), cancellationToken);

            if (!listed.IsSuccess)
            {
                Shut(nobodyHired: true, agent: null, trouble: listed.Error.Message);

                return;
            }

            AgentCard? stuck = null;
            string? trouble = null;

            foreach (var card in listed.Value.Where(card => card.Hired))
            {
                var readiness = await check.ExecuteAsync(new CheckAgent(card.Id), cancellationToken);

                if (readiness is { IsSuccess: true, Value: Ready })
                {
                    IsShut = false;

                    return;
                }

                if (stuck is null)
                {
                    stuck = card;
                    trouble = readiness.IsSuccess ? $"{readiness.Value.State} · {readiness.Value.Detail}" : readiness.Error.Message;
                }
            }

            Shut(nobodyHired: stuck is null, stuck?.Name, trouble);
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private void OpenAllay() => Problem = openAllay() ? null : "Allay isn't installed here; install it with just seed, then try again.";

    private void Shut(bool nobodyHired, string? agent, string? trouble)
    {
        (NobodyHired, Agent, Trouble) = (nobodyHired, agent, trouble);
        IsShut = true;
    }
}
