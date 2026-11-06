using System.ComponentModel;

namespace Overworld.Apps.Crafter.Projects;

public sealed record AddProject(
    [property: Description("The project's folder. Must exist.")]
    string Folder,
    [property: Description("How the project shows in Crafter. Leave it out to keep the name the project already has, or its folder's name.")]
    string? Name = null);
