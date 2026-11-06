using System.ComponentModel;

namespace Overworld.Apps.Crafter.Library;

public sealed record SetDefaultSkill(
    [property: Description("The id of a skill in the library.")]
    string Id,
    [property: Description("True for every project to get the skill, false for only the projects given it.")]
    bool IsDefault = true);
