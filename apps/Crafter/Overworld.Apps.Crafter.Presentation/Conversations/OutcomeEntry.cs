using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class OutcomeEntry(Outcome outcome) : Entry
{
    public string Text { get; } = outcome switch
    {
        Finished => "Finished",
        Stopped => "Stopped",
        CutShort => "Cut short: the reply ran out of room",
        Refused { Reason: { } reason } => $"Refused: {reason}",
        Refused => "Refused",
        Failed failed => $"Failed: {failed.Reason}",
        _ => outcome.GetType().Name,
    };

    public bool WentWrong { get; } = outcome is not (Finished or Stopped);
}
