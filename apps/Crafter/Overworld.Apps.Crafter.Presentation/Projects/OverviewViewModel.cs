using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Presentation.Markdown;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Repositories;
using Overworld.Apps.Crafter.Summaries;

namespace Overworld.Apps.Crafter.Presentation.Projects;

public sealed partial class OverviewViewModel(ProjectViewModel project, Abilities abilities) : ObservableObject
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public event EventHandler? Removed;

    public ProjectViewModel Project => project;

    public string Place => project.Project.Folder.StartsWith(Home, StringComparison.Ordinal)
        ? "~" + project.Project.Folder[Home.Length..]
        : project.Project.Folder;

    public ObservableCollection<SkillToggleViewModel> Skills { get; } = [];

    public bool HasSkills => Skills.Count > 0;

    [ObservableProperty]
    public partial string? Branch { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSummary), nameof(Blocks))]
    public partial string? Summary { get; private set; }

    public bool HasSummary => !string.IsNullOrWhiteSpace(Summary);

    public IReadOnlyList<Block> Blocks => Prose.Parse(Summary ?? string.Empty);

    [ObservableProperty]
    public partial bool IsEditing { get; private set; }

    [ObservableProperty]
    public partial string Draft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRenaming { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNameCommand))]
    public partial string NewName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRemoving { get; private set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        Branch = await Task.Run(() => Branches.Of(project.Project.Folder), cancellationToken);
        var summary = await abilities.ShowSummary.ExecuteAsync(new ShowSummary(project.Project.Folder), cancellationToken);
        Summary = summary.IsSuccess ? summary.Value.Text : null;
        Problem = summary.IsSuccess ? null : summary.Error.Message;
        await LoadSkillsAsync(cancellationToken);
    }

    [RelayCommand]
    public void StartRename()
    {
        NewName = project.Name;
        IsRenaming = true;
    }

    [RelayCommand]
    private void CancelRename() => IsRenaming = false;

    [RelayCommand(CanExecute = nameof(CanSaveName))]
    private async Task SaveNameAsync()
    {
        if (await EditAsync(new EditProject(project.Project.Folder, Name: NewName.Trim())))
        {
            IsRenaming = false;
        }
    }

    public Task SetIconAsync(string file) => EditAsync(new EditProject(project.Project.Folder, Icon: file));

    [RelayCommand]
    private async Task RemoveIconAsync() => await EditAsync(new EditProject(project.Project.Folder, RemoveIcon: true));

    [RelayCommand]
    private void Edit()
    {
        Draft = Summary ?? string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveSummaryAsync()
    {
        var written = await abilities.WriteSummary.ExecuteAsync(new WriteSummary(project.Project.Folder, Draft), CancellationToken.None);

        if (!written.IsSuccess)
        {
            Problem = written.Error.Message;

            return;
        }

        Problem = null;
        Summary = written.Value.Text;
        IsEditing = false;
        await abilities.Workshop.RebriefAsync(project.Project.Folder, CancellationToken.None);
    }

    [RelayCommand]
    private void AskToRemove() => IsRemoving = true;

    [RelayCommand]
    private void KeepProject() => IsRemoving = false;

    [RelayCommand]
    private async Task RemoveAsync()
    {
        var removed = await abilities.RemoveProject.ExecuteAsync(new RemoveProject(project.Project.Folder), CancellationToken.None);
        IsRemoving = false;

        if (!removed.IsSuccess)
        {
            Problem = removed.Error.Message;

            return;
        }

        Removed?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSaveName() => NewName.Trim().Length > 0;

    private async Task<bool> EditAsync(EditProject edit)
    {
        var edited = await abilities.EditProject.ExecuteAsync(edit, CancellationToken.None);

        if (!edited.IsSuccess)
        {
            Problem = edited.Error.Message;

            return false;
        }

        Problem = null;
        project.Renamed(edited.Value);
        OnPropertyChanged(nameof(Place));

        return true;
    }

    private async Task LoadSkillsAsync(CancellationToken cancellationToken)
    {
        var library = await abilities.ShowLibrary.ExecuteAsync(new ShowLibrary(), cancellationToken);
        var gets = await abilities.ListProjectSkills.ExecuteAsync(new ListProjectSkills(project.Project.Folder), cancellationToken);

        if (!library.IsSuccess || !gets.IsSuccess)
        {
            Problem = library.IsSuccess ? gets.Error.Message : library.Error.Message;

            return;
        }

        var got = gets.Value.Select(skill => skill.Id).ToHashSet();
        Skills.Clear();

        foreach (var skill in library.Value.Skills)
        {
            Skills.Add(new SkillToggleViewModel(skill, library.Value.Defaults.Contains(skill.Id), got.Contains(skill.Id), GiveAsync));
        }

        OnPropertyChanged(nameof(HasSkills));
    }

    private async Task GiveAsync(SkillToggleViewModel skill, bool given)
    {
        var gave = await abilities.GiveSkill.ExecuteAsync(new GiveSkill(project.Project.Folder, skill.Skill.Id, given), CancellationToken.None);

        if (!gave.IsSuccess)
        {
            Problem = gave.Error.Message;
            skill.Settle(!given);

            return;
        }

        Problem = null;
        await project.Aside.Skills.LoadAsync(CancellationToken.None);
        await abilities.Workshop.RebriefAsync(project.Project.Folder, CancellationToken.None);
    }
}
