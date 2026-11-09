using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Overworld.Web.Serving;

public static class ThemeSheet
{
    public static string Worn { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "overworld", "theme.json");

    public static string Css(string path)
    {
        var theme = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new InvalidDataException($"{path} holds no theme.");
        var css = new StringBuilder(":root {\n");
        var background = "#000000";

        foreach (var (name, color) in Section(theme, "colors"))
        {
            var hex = color!.GetValue<string>();
            css.Append(CultureInfo.InvariantCulture, $"  --{Kebab(name)}: {Rgba(hex)};\n");

            if (name == "background")
            {
                background = hex;
            }
        }

        if (theme["font"] is JsonObject font)
        {
            if (font["mono"] is JsonArray mono)
            {
                var families = mono.Select(family => family!.GetValue<string>()).Select(family => family.Contains(' ', StringComparison.Ordinal) ? $"\"{family}\"" : family);
                css.Append(CultureInfo.InvariantCulture, $"  --mono: {string.Join(", ", families)};\n");
            }

            foreach (var (name, size) in Section(font, "sizes"))
            {
                css.Append(CultureInfo.InvariantCulture, $"  --fs-{Kebab(name)}: {Number(size)}px;\n");
            }
        }

        foreach (var (name, radius) in Section(theme, "radii"))
        {
            css.Append(CultureInfo.InvariantCulture, $"  --r-{Kebab(name)}: {Number(radius)}px;\n");
        }

        foreach (var (name, space) in Section(theme, "space"))
        {
            css.Append(CultureInfo.InvariantCulture, $"  --s-{name}: {Number(space)}px;\n");
        }

        css.Append(CultureInfo.InvariantCulture, $"  color-scheme: {(IsLight(background) ? "light" : "dark")};\n}}\n");

        return css.ToString();
    }

    private static JsonObject Section(JsonObject node, string name) => node[name] as JsonObject ?? [];

    private static string Number(JsonNode? node) => node!.GetValue<double>().ToString(CultureInfo.InvariantCulture);

    private static string Rgba(string hex)
    {
        var digits = hex.TrimStart('#');

        if (digits.Length != 8)
        {
            return $"#{digits}";
        }

        var alpha = int.Parse(digits[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;

        return string.Create(CultureInfo.InvariantCulture, $"#{digits[2..]}{(int)Math.Round(alpha * 255):x2}");
    }

    private static bool IsLight(string hex)
    {
        var digits = hex.TrimStart('#');
        digits = digits.Length == 8 ? digits[2..] : digits;

        if (digits.Length != 6)
        {
            return false;
        }

        var red = int.Parse(digits[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var green = int.Parse(digits[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var blue = int.Parse(digits[4..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue) > 127.5;
    }

    private static string Kebab(string name)
    {
        var kebab = new StringBuilder();

        foreach (var letter in name)
        {
            if (char.IsUpper(letter))
            {
                kebab.Append('-').Append(char.ToLowerInvariant(letter));
            }
            else
            {
                kebab.Append(letter);
            }
        }

        return kebab.ToString();
    }
}
