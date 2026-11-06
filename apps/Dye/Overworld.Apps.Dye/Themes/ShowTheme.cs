using System.ComponentModel;

namespace Overworld.Apps.Dye.Themes;

public sealed record ShowTheme(
    [property: Description("Name of the theme, like amethyst.")]
    string Name);
