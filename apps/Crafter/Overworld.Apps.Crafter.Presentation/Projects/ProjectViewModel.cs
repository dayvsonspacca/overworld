using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Presentation.Aside;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Projects;

public sealed partial class ProjectViewModel : ObservableObject
{
    private readonly Abilities _abilities;

    private readonly List<ChatSummaryViewModel> _chats = [];

    public ProjectViewModel(Project project, Abilities abilities)
    {
        _abilities = abilities;
        Project = project;
        Aside = new AsideViewModel(project.Folder, abilities);
        Aside.Axioms.Changed += (_, _) => _ = abilities.Workshop.RebriefAsync(project.Folder, CancellationToken.None);
    }

    public Project Project { get; }

    public string Name => Project.Name;

    public string Letter => Project.Name.Length > 0 ? char.ToUpperInvariant(Project.Name[0]).ToString() : "?";

    public string? IconFile => Project.IconFile is { } file && File.Exists(file) ? file : null;

    public bool HasIcon => IconFile is not null;

    public ObservableCollection<ChatSummaryViewModel> Chats { get; } = [];

    public bool HasChats => _chats.Count > 0;

    public bool FindsNothing => HasChats && Chats.Count == 0;

    public AsideViewModel Aside { get; }

    [ObservableProperty]
    public partial ChatSummaryViewModel? Chat { get; set; }

    [ObservableProperty]
    public partial string Query { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Problem { get; set; }

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var chats = await _abilities.Chats.LoadAsync(Project.Folder, cancellationToken);

        if (chats.IsSuccess)
        {
            Problem = null;
            var known = _chats.ToDictionary(chat => chat.Id);
            var stored = chats.Value.Reverse().Select(record => known.GetValueOrDefault(record.Id) ?? Summary(record)).ToList();
            var unsaved = _chats.Where(chat => !stored.Contains(chat)).ToList();
            _chats.Clear();
            _chats.AddRange([.. unsaved, .. stored]);
            Filter();
            Chat = Chats.FirstOrDefault(chat => chat == Chat) ?? Chats.FirstOrDefault();
        }
        else
        {
            Problem = chats.Error.Message;
        }

        await Aside.LoadAsync(cancellationToken);
    }

    public async Task NewChatAsync(CancellationToken cancellationToken)
    {
        if (_chats.FirstOrDefault(chat => chat.IsFresh) is { } fresh)
        {
            Query = string.Empty;
            Chat = fresh;

            return;
        }

        var record = await _abilities.Workshop.FreshAsync(cancellationToken);

        if (!record.IsSuccess)
        {
            Problem = record.Error.Message;

            return;
        }

        Problem = null;
        var chat = new ChatSummaryViewModel(record.Value, Project.Folder, _abilities, fresh: true);
        _chats.Insert(0, chat);
        Query = string.Empty;
        Filter();
        Chat = chat;
    }

    public void Step(int by)
    {
        if (Chats.Count == 0)
        {
            return;
        }

        var at = Chat is null ? -1 : Chats.IndexOf(Chat);
        Chat = Chats[((at + by) % Chats.Count + Chats.Count) % Chats.Count];
    }

    partial void OnQueryChanged(string value) => Filter();

    partial void OnChatChanged(ChatSummaryViewModel? oldValue, ChatSummaryViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsOpen = false;
        }

        if (newValue is not null)
        {
            newValue.IsOpen = IsCurrent;
        }
    }

    partial void OnIsCurrentChanged(bool value)
    {
        if (Chat is not null)
        {
            Chat.IsOpen = value;
        }
    }

    private ChatSummaryViewModel Summary(ChatRecord record) => new(record, Project.Folder, _abilities);

    private void Filter()
    {
        var words = Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Chats.Clear();

        foreach (var chat in _chats.Where(chat => words.All(word => chat.Title.Contains(word, StringComparison.CurrentCultureIgnoreCase))))
        {
            Chats.Add(chat);
        }

        OnPropertyChanged(nameof(HasChats));
        OnPropertyChanged(nameof(FindsNothing));
    }
}
