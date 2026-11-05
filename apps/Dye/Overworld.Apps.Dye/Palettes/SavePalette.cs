using System.ComponentModel;

namespace Overworld.Apps.Dye.Palettes;

public sealed record SavePalette(
    [property: Description("Name of the palette. Must not be taken by another palette.")]
    string Name,
    [property: Description("Colors in order, as hex like #003F5C.")]
    IReadOnlyList<string> Colors);
