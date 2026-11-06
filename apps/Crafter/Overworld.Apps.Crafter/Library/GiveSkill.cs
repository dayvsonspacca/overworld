using System.ComponentModel;

namespace Overworld.Apps.Crafter.Library;

public sealed record GiveSkill(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The id of a skill in the library.")]
    string Skill,
    [property: Description("False takes the skill back from the project.")]
    bool Given = true);
