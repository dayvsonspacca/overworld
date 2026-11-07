using System.ComponentModel;

namespace Overworld.Apps.Dye.Themes;

public sealed record SaveTheme(
    [property: Description("Name of the new theme: lowercase letters, digits and dashes, like deep-sea. Must not be taken.")]
    string Name,
    [property: Description("Theme to start from, like amethyst; the new one keeps both its variants, fonts, radii, spacing and every color not changed.")]
    string From,
    [property: Description("Colors to change in the dark variant, as role=hex like accent=#3A7BD5; hex is #RRGGBB, or #AARRGGBB with transparency.")]
    IReadOnlyList<string>? Dark = null,
    [property: Description("Colors to change in the light variant, as role=hex like accent=#2F62B0; hex is #RRGGBB, or #AARRGGBB with transparency.")]
    IReadOnlyList<string>? Light = null);
