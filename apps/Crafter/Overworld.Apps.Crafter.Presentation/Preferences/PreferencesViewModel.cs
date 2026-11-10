using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Presentation.Preferences;

public sealed partial class PreferencesViewModel(Abilities abilities) : ObservableObject
{
    public event EventHandler? Changed;

    public ObservableCollection<DefaultSkillViewModel> Skills { get; } = [];

    public ObservableCollection<DefaultSkillViewModel> Shown { get; } = [];

    public ObservableCollection<string> Problems { get; } = [];

    public bool HasSkills => Skills.Count > 0;

    public bool HasProblems => Problems.Count > 0;

    public int DefaultCount => Skills.Count(skill => skill.IsDefault);

    [ObservableProperty]
    public partial string Query { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SkillFilter Filter { get; set; }

    [ObservableProperty]
    public partial string ProjectsRoot { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLinked))]
    public partial string? Repository { get; private set; }

    public bool IsLinked => Repository is not null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkCommand))]
    public partial string Draft { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LinkCommand), nameof(SyncCommand), nameof(UnlinkCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var preferences = await abilities.ShowPreferences.ExecuteAsync(new ShowPreferences(), cancellationToken);

        if (preferences.IsSuccess)
        {
            ProjectsRoot = preferences.Value.ProjectsRoot ?? string.Empty;
        }

        Show(await abilities.ShowLibrary.ExecuteAsync(new ShowLibrary(), cancellationToken));
    }

    public async Task SetProjectsRootAsync(string folder)
    {
        var set = await abilities.SetProjectsRoot.ExecuteAsync(new SetProjectsRoot(folder), CancellationToken.None);
        Problem = set.IsSuccess ? null : set.Error.Message;

        if (set.IsSuccess)
        {
            ProjectsRoot = set.Value.ProjectsRoot ?? folder;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLink))]
    private Task LinkAsync() => BusyAsync(() => abilities.LinkLibrary.ExecuteAsync(new LinkLibrary(Draft.Trim()), CancellationToken.None));

    [RelayCommand(CanExecute = nameof(CanTouch))]
    private Task SyncAsync() => BusyAsync(() => abilities.SyncLibrary.ExecuteAsync(new SyncLibrary(), CancellationToken.None));

    [RelayCommand(CanExecute = nameof(CanTouch))]
    private Task UnlinkAsync() => BusyAsync(() => abilities.LinkLibrary.ExecuteAsync(new LinkLibrary(string.Empty), CancellationToken.None));

    [RelayCommand]
    private void OpenAllay() => Problem = abilities.OpenAllay() ? null : "Allay isn't installed here; install it with just seed, then try again.";

    partial void OnQueryChanged(string value) => Narrow();

    partial void OnFilterChanged(SkillFilter value) => Narrow();

    private void Narrow()
    {
        var words = Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Shown.Clear();

        foreach (var skill in Skills.Where(skill => Passes(skill) && words.All(word => Mentions(skill, word))))
        {
            Shown.Add(skill);
        }
    }

    private bool Passes(DefaultSkillViewModel skill) => Filter switch
    {
        SkillFilter.On => skill.IsDefault,
        SkillFilter.Off => !skill.IsDefault,
        _ => true,
    };

    private static bool Mentions(DefaultSkillViewModel skill, string word) =>
        skill.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
        || skill.Description.Contains(word, StringComparison.OrdinalIgnoreCase)
        || skill.WhenToUse?.Contains(word, StringComparison.OrdinalIgnoreCase) is true;

    private bool CanLink() => !IsBusy && Draft.Trim().Length > 0;

    private bool CanTouch() => !IsBusy;

    private async Task BusyAsync(Func<Task<Result<LibraryState>>> change)
    {
        IsBusy = true;

        try
        {
            if (Show(await change()))
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool Show(Result<LibraryState> state)
    {
        if (!state.IsSuccess)
        {
            Problem = state.Error.Message;

            return false;
        }

        Problem = null;
        Repository = state.Value.Repository;
        Draft = Repository ?? string.Empty;
        Skills.Clear();

        foreach (var skill in state.Value.Skills)
        {
            Skills.Add(new DefaultSkillViewModel(skill, state.Value.Defaults.Contains(skill.Id), SetDefaultAsync));
        }

        Problems.Clear();

        foreach (var problem in state.Value.Problems)
        {
            Problems.Add($"{problem.Place}: {problem.Reason}");
        }

        Narrow();
        OnPropertyChanged(nameof(HasSkills));
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(DefaultCount));

        return true;
    }

    private async Task SetDefaultAsync(DefaultSkillViewModel skill, bool isDefault)
    {
        OnPropertyChanged(nameof(DefaultCount));
        var set = await abilities.SetDefaultSkill.ExecuteAsync(new SetDefaultSkill(skill.Skill.Id, isDefault), CancellationToken.None);

        if (!set.IsSuccess)
        {
            Problem = set.Error.Message;
            skill.Settle(!isDefault);
            OnPropertyChanged(nameof(DefaultCount));

            return;
        }

        Problem = null;
        Narrow();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
