using System.Globalization;
using System.Text.Json.Serialization;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Colors;

[JsonConverter(typeof(ColorJsonConverter))]
public sealed record Color(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";

    public double Lightness
    {
        get
        {
            var luminance = 0.2126 * Linear(R) + 0.7152 * Linear(G) + 0.0722 * Linear(B);

            return luminance > 216.0 / 24389 ? 116 * Math.Cbrt(luminance) - 16 : 24389.0 / 27 * luminance;
        }
    }

    public bool IsLight => Lightness > 60;

    public static Result<Color> Parse(string value)
    {
        var digits = value.StartsWith('#') ? value[1..] : value;

        if (digits.Length != 6
            || !int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb))
        {
            return new Error("color.invalid", $"'{value}' is not a hex color like #003F5C.");
        }

        return new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    public override string ToString() => Hex;

    private static double Linear(byte channel)
    {
        var value = channel / 255.0;

        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
