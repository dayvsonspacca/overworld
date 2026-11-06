using System.ComponentModel;

namespace Overworld.Apps.Dye.Themes;

public sealed record ApplyTheme(
    [property: Description("Name of the theme the shell and the apps should wear, like amethyst.")]
    string Name);
