using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Folders;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Projects;

public sealed partial class AddProjectViewModel(Abilities abilities, RepositoryFinder finder) : ObservableObject
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private IReadOnlyList<RepositoryChoice> _found = [];

    public event EventHandler<Project>? Added;

    public ObservableCollection<RepositoryChoice> Repositories { get; } = [];

    [ObservableProperty]
    public partial bool IsOpen { get; private set; }

    [ObservableProperty]
    public partial bool IsSearching { get; private set; }

    [ObservableProperty]
    public partial string Root { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Query { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial RepositoryChoice? Selected { get; set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    public bool FoundNone => !IsSearching && Repositories.Count == 0;

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        IsOpen = true;
        Query = string.Empty;
        Problem = null;
        IsSearching = true;
        OnPropertyChanged(nameof(FoundNone));

        var preferences = await abilities.ShowPreferences.ExecuteAsync(new ShowPreferences(), cancellationToken);
        var projects = await abilities.ListProjects.ExecuteAsync(new ListProjects(), cancellationToken);
        Root = preferences.IsSuccess ? preferences.Value.ProjectsRoot ?? Home : Home;
        var open = projects.IsSuccess ? projects.Value.Select(project => project.Folder).ToHashSet() : [];

        _found = await Task.Run(
            () => finder.Find(Root, cancellationToken)
                .Where(folder => !open.Contains(folder))
                .Select(folder => new RepositoryChoice(folder, Placed(Path.GetDirectoryName(folder) ?? folder)))
                .OrderBy(choice => choice.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            cancellationToken);

        IsSearching = false;
        Filter();
    }

    public async Task AddAsync(string folder)
    {
        var added = await abilities.AddProject.ExecuteAsync(new AddProject(folder), CancellationToken.None);

        if (!added.IsSuccess)
        {
            Problem = added.Error.Message;

            return;
        }

        IsOpen = false;
        Added?.Invoke(this, added.Value);
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private Task AddAsync() => AddAsync(Selected!.Folder);

    [RelayCommand]
    private void Close() => IsOpen = false;

    private bool CanAdd() => Selected is not null;

    partial void OnQueryChanged(string value) => Filter();

    private void Filter()
    {
        var words = Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Repositories.Clear();

        foreach (var choice in _found.Where(choice => words.All(word => choice.Folder.Contains(word, StringComparison.OrdinalIgnoreCase))))
        {
            Repositories.Add(choice);
        }

        Selected = Repositories.FirstOrDefault();
        OnPropertyChanged(nameof(FoundNone));
    }

    private static string Placed(string folder) =>
        folder.StartsWith(Home, StringComparison.Ordinal) ? "~" + folder[Home.Length..] : folder;
}
