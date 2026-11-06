using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Presentation.Preferences;
using Overworld.Apps.Crafter.Presentation.Projects;
using Overworld.Apps.Crafter.Folders;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Workbench;

public sealed partial class WorkbenchViewModel : ObservableObject
{
    private readonly Abilities abilities;

    public WorkbenchViewModel(Abilities abilities, RepositoryFinder? finder = null)
    {
        this.abilities = abilities;
        Adding = new AddProjectViewModel(abilities, finder ?? new RepositoryFinder());
        Adding.Added += (_, project) =>
        {
            var shown = Shown(project);
            Projects.Add(shown);
            Project = shown;
        };
        Preferences = new PreferencesViewModel(abilities);
        Preferences.Changed += (_, _) => _ = RebriefAllAsync();
    }

    public PreferencesViewModel Preferences { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Subtitle))]
    public partial bool ShowsPreferences { get; private set; }

    public AddProjectViewModel Adding { get; }

    public const int Slots = 9;

    public ObservableCollection<ProjectViewModel> Projects { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Subtitle))]
    public partial ProjectViewModel? Project { get; set; }

    [ObservableProperty]
    public partial bool ShowsList { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowsAside { get; set; } = true;

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public Task Opening { get; private set; } = Task.CompletedTask;

    public string Title => ShowsPreferences ? "Preferences" : Project is { ShowsOverview: false, Chat: { } chat } ? chat.Title : Project?.Name ?? "Crafter";

    public string? Subtitle => ShowsPreferences ? null : Project switch
    {
        { ShowsOverview: true } => "Project",
        { Chat: not null } project => project.Name,
        _ => null,
    };

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var listed = await abilities.ListProjects.ExecuteAsync(new ListProjects(), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        Problem = null;
        Projects.Clear();

        foreach (var project in listed.Value)
        {
            Projects.Add(Shown(project));
        }

        Project = Projects.FirstOrDefault();
        await Opening;
    }

    [RelayCommand]
    public void Go(int slot)
    {
        if (slot >= 0 && slot < Projects.Count)
        {
            Open(Projects[slot]);
        }
    }

    [RelayCommand]
    public void Open(ProjectViewModel project)
    {
        ShowsPreferences = false;
        Project = project;
    }

    public async Task MoveProjectAsync(int from, int to, CancellationToken cancellationToken)
    {
        if (from == to || from < 0 || from >= Projects.Count || to < 0 || to >= Projects.Count)
        {
            return;
        }

        var project = Projects[from];
        Projects.Move(from, to);
        var moved = await abilities.MoveProject.ExecuteAsync(new MoveProject(project.Project.Folder, to), cancellationToken);
        Problem = moved.IsSuccess ? null : moved.Error.Message;
    }

    [RelayCommand]
    public async Task TogglePreferencesAsync()
    {
        ShowsPreferences = !ShowsPreferences;

        if (ShowsPreferences)
        {
            await Preferences.LoadAsync(CancellationToken.None);
        }
    }

    private async Task RebriefAllAsync()
    {
        foreach (var project in Projects)
        {
            await abilities.Workshop.RebriefAsync(project.Project.Folder, CancellationToken.None);
        }

        if (Project is not null)
        {
            await Project.Aside.Skills.LoadAsync(CancellationToken.None);
        }
    }

    [RelayCommand]
    public Task AddProjectAsync() => Adding.OpenAsync(CancellationToken.None);

    [RelayCommand]
    public Task OpenOverviewAsync() => Project?.OpenOverviewAsync(CancellationToken.None) ?? Task.CompletedTask;

    [RelayCommand]
    public async Task RenameAsync()
    {
        if (Project is { } project)
        {
            await project.OpenOverviewAsync(CancellationToken.None);
            project.Overview.StartRename();
        }
    }

    private ProjectViewModel Shown(Project project)
    {
        var shown = new ProjectViewModel(project, abilities);
        shown.Overview.Removed += (_, _) => Remove(shown);

        return shown;
    }

    private void Remove(ProjectViewModel project)
    {
        foreach (var chat in project.Chats)
        {
            chat.Close();
        }

        var at = Projects.IndexOf(project);
        Projects.Remove(project);

        if (Project == project)
        {
            Project = Projects.Count == 0 ? null : Projects[Math.Min(at, Projects.Count - 1)];
        }
    }

    [RelayCommand]
    public Task NewChatAsync() => Project?.NewChatAsync(CancellationToken.None) ?? Task.CompletedTask;

    [RelayCommand]
    public void NextChat() => Project?.Step(1);

    [RelayCommand]
    public void PreviousChat() => Project?.Step(-1);

    [RelayCommand]
    public void ShowJobs()
    {
        if (Project?.Aside is { Conversation.HasJobs: true } aside)
        {
            ShowsAside = true;
            aside.ShowJobs();
        }
    }

    [RelayCommand]
    public void ToggleList() => ShowsList = !ShowsList;

    [RelayCommand]
    public void ToggleAside() => ShowsAside = !ShowsAside;

    partial void OnProjectChanged(ProjectViewModel? oldValue, ProjectViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsCurrent = false;
            oldValue.PropertyChanged -= ProjectChanged;
        }

        if (newValue is not null)
        {
            newValue.IsCurrent = true;
            newValue.PropertyChanged += ProjectChanged;
            Watch(newValue.Chat);
            Opening = newValue.LoadAsync(CancellationToken.None);
        }
    }

    private ChatSummaryViewModel? _watched;

    private void ProjectChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ProjectViewModel.Chat):
                Watch(Project?.Chat);
                break;
            case nameof(ProjectViewModel.ShowsOverview) or nameof(ProjectViewModel.Name):
                Retitle();
                break;
        }
    }

    private void Watch(ChatSummaryViewModel? chat)
    {
        if (_watched is not null)
        {
            _watched.PropertyChanged -= ChatChanged;
        }

        _watched = chat;

        if (chat is not null)
        {
            chat.PropertyChanged += ChatChanged;
        }

        Retitle();
    }

    private void ChatChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatSummaryViewModel.Title))
        {
            Retitle();
        }
    }

    private void Retitle()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
    }

    public void CloseAll()
    {
        foreach (var chat in Projects.SelectMany(project => project.Chats))
        {
            chat.Close();
        }
    }
}
