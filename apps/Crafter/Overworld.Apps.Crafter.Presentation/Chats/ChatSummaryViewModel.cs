using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Chats;

public sealed partial class ChatSummaryViewModel(ChatRecord record) : ObservableObject
{
    private const int Longest = 60;

    private ConversationViewModel? _conversation;

    public ChatRecord Record { get; } = record;

    public ConversationViewModel Conversation => _conversation ??= new ConversationViewModel(Record);

    public string Title => Record.Title.Text;

    public string LastLine => LastLineOf(Record.Turns);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking), nameof(IsUnread))]
    public partial ChatState State { get; set; }

    public bool IsWorking => State == ChatState.Working;

    public bool IsUnread => State == ChatState.Unread;

    private static string LastLineOf(IReadOnlyList<Turn> turns)
    {
        var said = turns.OfType<Reply>().LastOrDefault()?.Moments.OfType<Speech>().LastOrDefault()?.Text
            ?? turns.OfType<Prompt>().LastOrDefault()?.Text
            ?? string.Empty;
        var line = said.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? string.Empty;

        return line.Length <= Longest ? line : line[..(Longest - 1)].TrimEnd() + "…";
    }
}
