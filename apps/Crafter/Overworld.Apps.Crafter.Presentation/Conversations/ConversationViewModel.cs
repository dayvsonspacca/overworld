using Overworld.Apps.Crafter.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class ConversationViewModel
{
    public ConversationViewModel(ChatRecord record)
    {
        foreach (var turn in record.Turns)
        {
            Timeline.Add(turn);
        }
    }

    public Timeline Timeline { get; } = new();
}
