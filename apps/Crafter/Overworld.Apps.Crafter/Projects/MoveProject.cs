using System.ComponentModel;

namespace Overworld.Apps.Crafter.Projects;

public sealed record MoveProject(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("Where the project goes among the open ones, counting from 0 at the top of the rail.")]
    int To);
