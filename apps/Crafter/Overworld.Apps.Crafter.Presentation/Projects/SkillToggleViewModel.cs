using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Library;

namespace Overworld.Apps.Crafter.Presentation.Projects;

public sealed partial class SkillToggleViewModel : ObservableObject
{
    private readonly Func<SkillToggleViewModel, bool, Task> _give;

    private bool _settling;

    public SkillToggleViewModel(Skill skill, bool isDefault, bool given, Func<SkillToggleViewModel, bool, Task> give)
    {
        _give = give;
        Skill = skill;
        IsDefault = isDefault;
        _settling = true;
        Gets = isDefault || given;
        _settling = false;
    }

    public Skill Skill { get; }

    public string Name => Skill.Name;

    public string Description => IsDefault ? $"{Skill.Description} Every project gets it." : Skill.Description;

    public bool IsDefault { get; }

    public bool CanChange => !IsDefault;

    [ObservableProperty]
    public partial bool Gets { get; set; }

    internal void Settle(bool gets)
    {
        _settling = true;
        Gets = gets;
        _settling = false;
    }

    partial void OnGetsChanged(bool value)
    {
        if (!_settling)
        {
            _ = _give(this, value);
        }
    }
}
