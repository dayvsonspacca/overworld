using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class OutcomeEntry(Outcome outcome, TimeSpan? took = null) : Entry
{
    public string Text { get; } = took is { } spent ? $"{Said(outcome)} · {ThoughtEntry.Spoken(spent)}" : Said(outcome);

    public bool WentWrong { get; } = outcome is not (Finished or Stopped);

    private static string Said(Outcome outcome) => outcome switch
    {
        Finished => "Finished",
        Stopped => "Stopped",
        CutShort => "Cut short: the reply ran out of room",
        Refused { Reason: { } reason } => $"Refused: {reason}",
        Refused => "Refused",
        Failed failed => $"Failed: {failed.Reason}",
        _ => outcome.GetType().Name,
    };
}
