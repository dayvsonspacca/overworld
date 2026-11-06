using System.ComponentModel;

namespace Overworld.Apps.Crafter.Projects;

public sealed record EditProject(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The project's new name. Leave it out to keep the name.")]
    string? Name = null,
    [property: Description("An image to become the project's icon; it is copied into the project's .crafter folder. Leave it out to keep the icon.")]
    string? Icon = null,
    [property: Description("True to take the project's icon away.")]
    bool RemoveIcon = false);
