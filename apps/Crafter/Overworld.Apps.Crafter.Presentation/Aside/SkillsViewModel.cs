using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Library;

namespace Overworld.Apps.Crafter.Presentation.Aside;

public sealed partial class SkillsViewModel(string folder, Abilities abilities) : ObservableObject
{
    public ObservableCollection<Skill> Skills { get; } = [];

    public bool HasSkills => Skills.Count > 0;

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var listed = await abilities.ListProjectSkills.ExecuteAsync(new ListProjectSkills(folder), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        Problem = null;
        Skills.Clear();

        foreach (var skill in listed.Value)
        {
            Skills.Add(skill);
        }

        OnPropertyChanged(nameof(HasSkills));
    }
}
