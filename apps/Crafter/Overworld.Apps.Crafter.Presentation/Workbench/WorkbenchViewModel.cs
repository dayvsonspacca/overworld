using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Presentation.Projects;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Workbench;

public sealed partial class WorkbenchViewModel(Abilities abilities) : ObservableObject
{
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

    public string Title => Project?.Chat?.Title ?? Project?.Name ?? "Crafter";

    public string? Subtitle => Project?.Chat is null ? null : Project.Name;

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
            Projects.Add(new ProjectViewModel(project, abilities));
        }

        Project = Projects.FirstOrDefault();
        await Opening;
    }

    [RelayCommand]
    public void Go(int slot)
    {
        if (slot >= 0 && slot < Projects.Count)
        {
            Project = Projects[slot];
        }
    }

    [RelayCommand]
    public void Open(ProjectViewModel project) => Project = project;

    [RelayCommand]
    public Task NewChatAsync() => Project?.NewChatAsync(CancellationToken.None) ?? Task.CompletedTask;

    [RelayCommand]
    public void NextChat() => Project?.Step(1);

    [RelayCommand]
    public void PreviousChat() => Project?.Step(-1);

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
        if (e.PropertyName == nameof(ProjectViewModel.Chat))
        {
            Watch(Project?.Chat);
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
