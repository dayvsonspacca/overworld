using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Library;

namespace Overworld.Apps.Crafter.Presentation.Preferences;

public sealed partial class DefaultSkillViewModel : ObservableObject
{
    private readonly Func<DefaultSkillViewModel, bool, Task> _set;

    private bool _settling;

    public DefaultSkillViewModel(Skill skill, bool isDefault, Func<DefaultSkillViewModel, bool, Task> set)
    {
        _set = set;
        Skill = skill;
        Settle(isDefault);
    }

    public Skill Skill { get; }

    public string Name => Skill.Name;

    public string Description => Skill.Description;

    [ObservableProperty]
    public partial bool IsDefault { get; set; }

    internal void Settle(bool isDefault)
    {
        _settling = true;
        IsDefault = isDefault;
        _settling = false;
    }

    partial void OnIsDefaultChanged(bool value)
    {
        if (!_settling)
        {
            _ = _set(this, value);
        }
    }
}
