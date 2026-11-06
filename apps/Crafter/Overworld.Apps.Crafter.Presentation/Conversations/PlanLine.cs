using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed record PlanLine(PlanStep Step)
{
    public string Text => Step.Text;

    public string Glyph => Step.State switch
    {
        StepState.Done => "✓",
        StepState.Doing => "›",
        _ => "○",
    };

    public bool IsDone => Step.State == StepState.Done;

    public bool IsDoing => Step.State == StepState.Doing;
}
