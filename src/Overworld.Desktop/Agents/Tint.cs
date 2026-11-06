using Avalonia.Media;

namespace Overworld.Desktop.Agents;

internal static class Tint
{
    private const double Colorless = 0.25;

    public static void Apply(Span<byte> pixels, Color target, bool bgra, bool premultiplied)
    {
        var (red, blue) = bgra ? (2, 0) : (0, 2);
        var (hue, saturation, value) = Measure(pixels, red, blue, premultiplied);

        if (value == 0)
        {
            return;
        }

        var (toHue, toSaturation, toValue) = Hsv(target.R / 255d, target.G / 255d, target.B / 255d);

        for (var at = 0; at < pixels.Length; at += 4)
        {
            var alpha = pixels[at + 3];

            if (alpha == 0)
            {
                continue;
            }

            var (h, s, v) = HsvAt(pixels, at, red, blue, premultiplied);
            var (r, g, b) = Rgb(
                ((h - hue + toHue) % 1 + 1) % 1,
                Math.Min(1, s * toSaturation / saturation),
                Math.Min(1, v * toValue / value));
            var scale = premultiplied ? alpha / 255d : 1;

            pixels[at + red] = (byte)Math.Round(r * 255 * scale);
            pixels[at + 1] = (byte)Math.Round(g * 255 * scale);
            pixels[at + blue] = (byte)Math.Round(b * 255 * scale);
        }
    }

    private static (double Hue, double Saturation, double Value) Measure(ReadOnlySpan<byte> pixels, int red, int blue, bool premultiplied)
    {
        double hue = 0, saturation = 0, value = 0;
        var counted = 0;

        for (var at = 0; at < pixels.Length; at += 4)
        {
            if (pixels[at + 3] == 0)
            {
                continue;
            }

            var (h, s, v) = HsvAt(pixels, at, red, blue, premultiplied);

            if (s > Colorless)
            {
                (hue, saturation, value) = (hue + h, saturation + s, value + v);
                counted++;
            }
        }

        return counted == 0 ? (0, 0, 0) : (hue / counted, saturation / counted, value / counted);
    }

    private static (double Hue, double Saturation, double Value) HsvAt(ReadOnlySpan<byte> pixels, int at, int red, int blue, bool premultiplied)
    {
        var scale = premultiplied ? 255d / pixels[at + 3] : 1;

        return Hsv(
            Math.Min(1, pixels[at + red] * scale / 255),
            Math.Min(1, pixels[at + 1] * scale / 255),
            Math.Min(1, pixels[at + blue] * scale / 255));
    }

    private static (double Hue, double Saturation, double Value) Hsv(double r, double g, double b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var spread = max - Math.Min(r, Math.Min(g, b));
        var hue = spread == 0 ? 0
            : max == r ? (g - b) / spread % 6
            : max == g ? (b - r) / spread + 2
            : (r - g) / spread + 4;

        return ((hue / 6 % 1 + 1) % 1, max == 0 ? 0 : spread / max, max);
    }

    private static (double R, double G, double B) Rgb(double hue, double saturation, double value)
    {
        var sector = (int)Math.Floor(hue * 6);
        var part = hue * 6 - sector;
        var (low, falling, rising) = (value * (1 - saturation), value * (1 - part * saturation), value * (1 - (1 - part) * saturation));

        return (sector % 6) switch
        {
            0 => (value, rising, low),
            1 => (falling, value, low),
            2 => (low, value, rising),
            3 => (low, falling, value),
            4 => (rising, low, value),
            _ => (value, low, falling),
        };
    }
}
