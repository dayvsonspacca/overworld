using System.ComponentModel;

namespace Overworld.Apps.Crafter.Preferences;

public sealed record SetProjectsRoot(
    [property: Description("The folder where the folder picker starts and new projects are made. Must exist.")]
    string Folder);
