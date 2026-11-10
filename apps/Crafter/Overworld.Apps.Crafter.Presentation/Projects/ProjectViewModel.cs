using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Presentation.Aside;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Repositories;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Presentation.Projects;

public sealed partial class ProjectViewModel : ObservableObject
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private readonly Abilities _abilities;

    private readonly List<ChatSummaryViewModel> _chats = [];

    private readonly SynchronizationContext? _ui = SynchronizationContext.Current;

    public ProjectViewModel(Project project, Abilities abilities)
    {
        _abilities = abilities;
        Project = project;
        Overview = new OverviewViewModel(this, abilities);
        Aside = new AsideViewModel(project.Folder, abilities);
        Aside.Axioms.Changed += (_, _) => _ = abilities.Workshop.RebriefAsync(project.Folder, CancellationToken.None);
        abilities.Workshop.Changed += (_, folder) =>
        {
            if (folder == project.Folder)
            {
                OnUi(() => _ = RefreshAsync(CancellationToken.None));
            }
        };
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(Letter), nameof(IconFile), nameof(HasIcon), nameof(Place))]
    public partial Project Project { get; private set; }

    public OverviewViewModel Overview { get; }

    [ObservableProperty]
    public partial bool ShowsOverview { get; private set; }

    public string Name => Project.Name;

    public string Letter => Project.Name.Length > 0 ? char.ToUpperInvariant(Project.Name[0]).ToString() : "?";

    public string? IconFile => Project.IconFile is { } file && File.Exists(file) ? file : null;

    public bool HasIcon => IconFile is not null;

    public string Place => Project.Folder.StartsWith(Home, StringComparison.Ordinal)
        ? "~" + Project.Folder[Home.Length..]
        : Project.Folder;

    [ObservableProperty]
    public partial string? Branch { get; private set; }

    public event EventHandler? Tallied;

    public IReadOnlyList<ChatSummaryViewModel> All => _chats;

    public int ChatCount => _chats.Count;

    public int Working => _chats.Count(chat => chat.IsWorking);

    public int Waiting => _chats.Count(chat => chat.IsWaiting);

    public string? LastLine => (_chats.Where(chat => chat.Touched is not null).MaxBy(chat => chat.Touched) ?? _chats.FirstOrDefault(chat => !chat.IsFresh))?.LastLine is { Length: > 0 } line ? line : null;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemovingChat))]
    public partial ChatSummaryViewModel? Removing { get; private set; }

    public bool IsRemovingChat => Removing is not null;

    public bool CanArrange => Query.Length == 0;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (await ListAsync(cancellationToken))
        {
            Chat = Chats.FirstOrDefault(chat => chat == Chat) ?? Chats.FirstOrDefault();
        }

        await Aside.LoadAsync(cancellationToken);
    }

    public async Task SurveyAsync(CancellationToken cancellationToken)
    {
        await ReadBranchAsync(cancellationToken);
        await ListAsync(cancellationToken);
        await Aside.Todos.LoadAsync(cancellationToken);
    }

    public async Task ReadBranchAsync(CancellationToken cancellationToken) =>
        Branch = await Task.Run(() => Branches.Of(Project.Folder), cancellationToken);

    private async Task<bool> ListAsync(CancellationToken cancellationToken)
    {
        var chats = await _abilities.Chats.LoadAsync(Project.Folder, cancellationToken);

        if (!chats.IsSuccess)
        {
            Problem = chats.Error.Message;

            return false;
        }

        Problem = null;
        var known = _chats.ToDictionary(chat => chat.Id);
        var stored = chats.Value.Reverse().Select(record => known.GetValueOrDefault(record.Id) ?? Summary(record)).ToList();
        var unsaved = _chats.Where(chat => !stored.Contains(chat)).ToList();
        _chats.Clear();
        _chats.AddRange([.. unsaved, .. stored]);
        Filter();

        return true;
    }

    public async Task NewChatAsync(CancellationToken cancellationToken)
    {
        var fresh = await FreshChatAsync(cancellationToken);
        Problem = fresh.IsSuccess ? null : fresh.Error.Message;
    }

    public async Task<Result<ChatSummaryViewModel>> FreshChatAsync(CancellationToken cancellationToken)
    {
        if (_chats.FirstOrDefault(chat => chat.IsFresh) is { } fresh)
        {
            Query = string.Empty;
            Chat = fresh;

            return fresh;
        }

        var record = await _abilities.Workshop.FreshAsync(cancellationToken);

        if (!record.IsSuccess)
        {
            return record.Error;
        }

        var chat = Watched(new ChatSummaryViewModel(record.Value, Project.Folder, _abilities, fresh: true));
        _chats.Insert(0, chat);
        Query = string.Empty;
        Filter();
        Chat = chat;

        return chat;
    }

    public void Edited(Project project)
    {
        Project = project;
        OnPropertyChanged(nameof(IconFile));
    }

    [RelayCommand]
    private void AskToRemoveChat(ChatSummaryViewModel chat) => Removing = chat;

    [RelayCommand]
    private void KeepChat() => Removing = null;

    [RelayCommand]
    private async Task RemoveChatAsync()
    {
        if (Removing is not { } chat)
        {
            return;
        }

        Removing = null;
        chat.Forget();
        var removed = await _abilities.Chats.RemoveAsync(Project.Folder, chat.Id, CancellationToken.None);

        if (!removed.IsSuccess)
        {
            Problem = removed.Error.Message;

            return;
        }

        Problem = null;
        var at = Chats.IndexOf(chat);
        _chats.Remove(chat);
        Filter();

        if (Chat == chat)
        {
            Chat = Chats.Count == 0 ? null : Chats[Math.Clamp(at, 0, Chats.Count - 1)];
        }
    }

    public async Task MoveChatAsync(int from, int to, CancellationToken cancellationToken)
    {
        if (!CanArrange || from == to || from < 0 || from >= _chats.Count || to < 0 || to >= _chats.Count)
        {
            return;
        }

        var moved = _chats[from];
        var open = Chat;
        _chats.RemoveAt(from);
        _chats.Insert(to, moved);
        Chats.Move(from, to);
        Chat = open;
        var arranged = await _abilities.Chats.ArrangeAsync(Project.Folder, [.. _chats.Select(chat => chat.Id)], cancellationToken);
        Problem = arranged.IsSuccess ? null : arranged.Error.Message;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var projects = await _abilities.ListProjects.ExecuteAsync(new ListProjects(), cancellationToken);

        if (projects.IsSuccess && projects.Value.FirstOrDefault(each => each.Folder == Project.Folder) is { } current)
        {
            Edited(current);
        }

        await Aside.LoadAsync(cancellationToken);

        if (ShowsOverview)
        {
            await Overview.LoadAsync(cancellationToken);
        }
    }

    public async Task OpenOverviewAsync(CancellationToken cancellationToken)
    {
        ShowsOverview = true;
        await Overview.LoadAsync(cancellationToken);
    }

    public void Show(ChatId id)
    {
        Query = string.Empty;

        if (_chats.FirstOrDefault(chat => chat.Id == id) is { } chat)
        {
            Chat = chat;
            ShowsOverview = false;
        }
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

    partial void OnQueryChanged(string value)
    {
        Filter();
        OnPropertyChanged(nameof(CanArrange));
    }

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

        Aside.Conversation = newValue?.Conversation;

        if (newValue is not null)
        {
            ShowsOverview = false;
        }
    }

    partial void OnIsCurrentChanged(bool value)
    {
        if (Chat is not null)
        {
            Chat.IsOpen = value;
        }
    }

    private void OnUi(Action act)
    {
        if (_ui is null)
        {
            act();
        }
        else
        {
            _ui.Post(_ => act(), null);
        }
    }

    private ChatSummaryViewModel Summary(ChatRecord record) => Watched(new(record, Project.Folder, _abilities));

    private ChatSummaryViewModel Watched(ChatSummaryViewModel chat)
    {
        chat.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ChatSummaryViewModel.State) or nameof(ChatSummaryViewModel.IsWaiting) or nameof(ChatSummaryViewModel.Touched) or nameof(ChatSummaryViewModel.LastLine))
            {
                Tally();
            }
        };

        return chat;
    }

    private void Tally()
    {
        OnPropertyChanged(nameof(ChatCount));
        OnPropertyChanged(nameof(Working));
        OnPropertyChanged(nameof(Waiting));
        OnPropertyChanged(nameof(LastLine));
        Tallied?.Invoke(this, EventArgs.Empty);
    }

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
        Tally();
    }
}
