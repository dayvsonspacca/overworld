using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Agents;
using Overworld.Allay.Hiring;
using Overworld.Allay.Metering;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Allay.Presentation;

public sealed partial class AgentsViewModel(
    Operation<ListAgents, IReadOnlyList<AgentCard>> list,
    Operation<CheckAgent, Readiness> check,
    Operation<HireAgent, AgentCard> hire,
    Operation<ConfigureAgent, AgentCard> configure,
    Operation<MeasureAgent, Usage> measure,
    TimeProvider clock) : ObservableObject
{
    public ObservableCollection<AgentViewModel> Agents { get; } = [];

    [ObservableProperty]
    public partial AgentViewModel? Selected { get; set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    public string Headcount => $"{Agents.Count(agent => agent.IsHired)} of {Agents.Count} hired";

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var listed = await list.ExecuteAsync(new ListAgents(), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        Problem = null;
        var selected = Selected?.Id;
        Agents.Clear();

        foreach (var card in listed.Value)
        {
            var agent = new AgentViewModel(card, check, hire, configure, measure, clock);
            agent.PropertyChanged += Follow;
            Agents.Add(agent);
        }

        Selected = Agents.FirstOrDefault(agent => agent.Id == selected) ?? Agents.FirstOrDefault();
        OnPropertyChanged(nameof(Headcount));
        await CheckAllAsync(cancellationToken);
    }

    [RelayCommand]
    private Task CheckAllAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(Agents.Select(agent => agent.CheckAsync(cancellationToken)));

    private void Follow(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AgentViewModel.IsHired))
        {
            OnPropertyChanged(nameof(Headcount));
        }
    }
}
