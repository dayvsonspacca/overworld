using System.ComponentModel;

namespace Overworld.Apps.Dye.Palettes;

public sealed record ShowPalette(
    [property: Description("Name of the palette, exactly as it was saved.")]
    string Name);
