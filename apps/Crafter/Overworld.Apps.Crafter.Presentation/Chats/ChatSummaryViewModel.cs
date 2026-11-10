using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Presentation.Chats;

public sealed partial class ChatSummaryViewModel(ChatRecord record, string folder, Abilities abilities, bool fresh = false) : ObservableObject
{
    private const int Longest = 60;

    private ConversationViewModel? _conversation;

    private Task _desk = Task.CompletedTask;

    public ChatId Id => record.Id;

    public ChatRecord Record => _conversation?.Record ?? record;

    public bool IsFresh => fresh && Record.Turns.Count == 0 && _conversation is not { IsWorking: true };

    public ConversationViewModel Conversation
    {
        get
        {
            if (_conversation is null)
            {
                _conversation = new ConversationViewModel(folder, record, abilities) { IsShown = IsOpen };
                _conversation.PropertyChanged += Changed;
                _conversation.Answered += (_, _) => Heard();
                _desk = _conversation.LoadDeskAsync(CancellationToken.None);
            }

            return _conversation;
        }
    }

    public string Title => _conversation?.Title ?? record.Title.Text;

    public string LastLine => LastLineOf(Record.Turns);

    [ObservableProperty]
    public partial DateTimeOffset? Touched { get; private set; } = record.Touched;

    public bool IsWaiting => _conversation?.IsWaiting ?? false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking), nameof(IsUnread))]
    public partial ChatState State { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    public bool IsWorking => State == ChatState.Working;

    public bool IsUnread => State == ChatState.Unread;

    public async Task<Result<Prompt>> SayAsync(string draft)
    {
        var conversation = Conversation;
        await _desk;

        return conversation.Say(draft);
    }

    public void Close() => _conversation?.Close();

    public void Forget()
    {
        if (_conversation is null)
        {
            abilities.Workshop.Close(Id);
        }
        else
        {
            _conversation.Forget();
        }
    }

    partial void OnIsOpenChanged(bool value)
    {
        if (_conversation is not null)
        {
            _conversation.IsShown = value;
        }

        if (value && State == ChatState.Unread)
        {
            State = ChatState.Idle;
        }
    }

    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ConversationViewModel.Title):
                OnPropertyChanged(nameof(Title));
                break;
            case nameof(ConversationViewModel.IsWorking) when _conversation!.IsWorking:
                State = ChatState.Working;
                break;
            case nameof(ConversationViewModel.IsWaiting):
                OnPropertyChanged(nameof(IsWaiting));
                break;
        }
    }

    private void Heard()
    {
        State = IsOpen ? ChatState.Idle : ChatState.Unread;
        Touched = abilities.Time.GetUtcNow();
        OnPropertyChanged(nameof(LastLine));
    }

    private static string LastLineOf(IReadOnlyList<Turn> turns)
    {
        var said = turns.OfType<Reply>().LastOrDefault()?.Moments.OfType<Speech>().LastOrDefault()?.Text
            ?? turns.OfType<Prompt>().LastOrDefault()?.Text
            ?? string.Empty;
        var line = said.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? string.Empty;

        return line.Length <= Longest ? line : line[..(Longest - 1)].TrimEnd() + "…";
    }
}
