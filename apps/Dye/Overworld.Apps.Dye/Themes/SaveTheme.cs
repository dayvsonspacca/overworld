using System.ComponentModel;

namespace Overworld.Apps.Dye.Themes;

public sealed record SaveTheme(
    [property: Description("Name of the new theme: lowercase letters, digits and dashes, like deep-sea. Must not be taken.")]
    string Name,
    [property: Description("Theme to start from, like amethyst; the new one keeps its fonts, radii, spacing and every color not changed.")]
    string From,
    [property: Description("Colors to change, as role=hex like accent=#3A7BD5; hex is #RRGGBB, or #AARRGGBB with transparency.")]
    IReadOnlyList<string>? Colors = null);
