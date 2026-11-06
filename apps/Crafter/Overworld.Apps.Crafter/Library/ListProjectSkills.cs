using System.ComponentModel;

namespace Overworld.Apps.Crafter.Library;

public sealed record ListProjectSkills(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder);
