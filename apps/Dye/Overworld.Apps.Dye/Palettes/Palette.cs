using Overworld.Apps.Dye.Colors;

namespace Overworld.Apps.Dye.Palettes;

public sealed record Palette(string Name, IReadOnlyList<Color> Colors);
