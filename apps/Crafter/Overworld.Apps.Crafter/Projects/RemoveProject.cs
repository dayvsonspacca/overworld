using System.ComponentModel;

namespace Overworld.Apps.Crafter.Projects;

public sealed record RemoveProject(
    [property: Description("The folder of a project open in Crafter. It may be gone from disk.")]
    string Folder);
