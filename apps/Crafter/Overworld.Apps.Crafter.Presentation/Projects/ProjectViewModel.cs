using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Presentation.Aside;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Projects;

public sealed partial class ProjectViewModel(Project project, Abilities abilities) : ObservableObject
{
    private readonly List<ChatSummaryViewModel> _chats = [];

    public Project Project { get; } = project;

    public string Name => Project.Name;

    public string Letter => Project.Name.Length > 0 ? char.ToUpperInvariant(Project.Name[0]).ToString() : "?";

    public string? IconFile => Project.IconFile is { } file && File.Exists(file) ? file : null;

    public bool HasIcon => IconFile is not null;

    public ObservableCollection<ChatSummaryViewModel> Chats { get; } = [];

    public bool HasChats => _chats.Count > 0;

    public bool FindsNothing => HasChats && Chats.Count == 0;

    public AsideViewModel Aside { get; } = new(project.Folder, abilities);

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
        var chats = await abilities.Chats.LoadAsync(Project.Folder, cancellationToken);

        if (chats.IsSuccess)
        {
            Problem = null;
            _chats.Clear();
            _chats.AddRange(chats.Value.Reverse().Select(chat => new ChatSummaryViewModel(chat)));
            Filter();
            Chat = Chats.FirstOrDefault(chat => chat.Record.Id == Chat?.Record.Id) ?? Chats.FirstOrDefault();
        }
        else
        {
            Problem = chats.Error.Message;
        }

        await Aside.LoadAsync(cancellationToken);
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
