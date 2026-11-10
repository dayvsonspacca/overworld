using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Presentation.Projects;

namespace Overworld.Apps.Crafter.Presentation.Home;

public sealed partial class HomeViewModel : ObservableObject
{
    public const int Shown = 6;

    private readonly ObservableCollection<ProjectViewModel> _projects;

    private readonly HashSet<ProjectViewModel> _watched = [];

    private readonly Dictionary<ChatSummaryViewModel, RecentChatViewModel> _known = [];

    public HomeViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        _projects = projects;
        _projects.CollectionChanged += Rewatch;
        Rewatch(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public ObservableCollection<RecentChatViewModel> Recent { get; } = [];

    public bool HasRecent => Recent.Count > 0;

    public int Working => _projects.Sum(project => project.Working);

    public int Waiting => _projects.Sum(project => project.Waiting);

    public async Task SurveyAsync(CancellationToken cancellationToken)
    {
        foreach (var project in _projects.ToList())
        {
            await project.SurveyAsync(cancellationToken);
        }
    }

    private void Rewatch(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var gone in _watched.Except(_projects).ToList())
        {
            gone.Tallied -= Tallied;
            _watched.Remove(gone);
        }

        foreach (var project in _projects.Where(_watched.Add))
        {
            project.Tallied += Tallied;
        }

        Rank();
    }

    private void Tallied(object? sender, EventArgs e) => Rank();

    private void Rank()
    {
        var ranked = _projects
            .SelectMany(project => project.All.Where(chat => chat.Touched is not null).Select(chat => (project, chat)))
            .OrderByDescending(each => each.chat.Touched)
            .Take(Shown)
            .Select(each => _known.TryGetValue(each.chat, out var known) && known.Project == each.project
                ? known
                : _known[each.chat] = new RecentChatViewModel(each.project, each.chat))
            .ToList();

        foreach (var stale in _known.Keys.Where(chat => !ranked.Any(recent => recent.Chat == chat)).ToList())
        {
            _known.Remove(stale);
        }

        if (!Recent.SequenceEqual(ranked))
        {
            Recent.Clear();

            foreach (var recent in ranked)
            {
                Recent.Add(recent);
            }
        }

        OnPropertyChanged(nameof(HasRecent));
        OnPropertyChanged(nameof(Working));
        OnPropertyChanged(nameof(Waiting));
    }
}
