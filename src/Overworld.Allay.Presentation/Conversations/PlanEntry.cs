using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.Presentation.Conversations;

public sealed partial class PlanEntry(Plan plan) : Entry
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress), nameof(Lines))]
    public partial IReadOnlyList<PlanStep> Steps { get; set; } = plan.Steps;

    public string Progress => $"{Steps.Count(step => step.State == StepState.Done)}/{Steps.Count}";

    public IReadOnlyList<PlanLine> Lines => [.. Steps.Select(step => new PlanLine(step))];
}
