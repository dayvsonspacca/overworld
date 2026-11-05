using Overworld.Apps.Dye.Colors;

namespace Overworld.Apps.Dye.Presentation;

public sealed record Swatch(string Label, string Fill, string Ink)
{
    public static Swatch Of(Color color) => new(color.Hex[1..], color.Hex, color.IsLight ? "#000000" : "#FFFFFF");
}
