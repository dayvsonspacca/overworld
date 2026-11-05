using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Overworld.Desktop.Theming;

public sealed class ThemeResources : ResourceDictionary
{
    public ThemeResources()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "overworld", "theme.json"))
    {
    }

    public ThemeResources(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"There is no theme at {path}; run just seed to install one.", path);
        }

        var theme = JsonNode.Parse(File.ReadAllText(path))
            ?? throw new InvalidDataException($"{path} holds null, not a theme.");

        JsonObject Section(JsonNode node, string name) =>
            node[name] as JsonObject ?? throw new InvalidDataException($"{path} has no '{name}'.");

        foreach (var (name, value) in Section(theme, "colors"))
        {
            var color = Color.Parse(value!.GetValue<string>());

            Add($"{Pascal(name)}Color", color);
            Add($"{Pascal(name)}Brush", new SolidColorBrush(color));
        }

        var font = Section(theme, "font");
        var mono = font["mono"]?.AsArray().Select(family => family!.GetValue<string>())
            ?? throw new InvalidDataException($"{path} has no 'font.mono'.");

        Add("MonoFont", new FontFamily(string.Join(", ", mono)));

        foreach (var (name, size) in Section(font, "sizes"))
        {
            Add($"FontSize{Pascal(name)}", size!.GetValue<double>());
        }

        foreach (var (name, radius) in Section(theme, "radii"))
        {
            Add($"Radius{Pascal(name)}", new CornerRadius(radius!.GetValue<double>()));
        }

        foreach (var (name, space) in Section(theme, "space"))
        {
            Add($"Space{name}", space!.GetValue<double>());
        }

        var accent = (Color)this["AccentColor"]!;

        Add("FocusGlow", new BoxShadows(new BoxShadow { Spread = 3, Color = Color.FromArgb(0x38, accent.R, accent.G, accent.B) }));
    }

    private static string Pascal(string name) => char.ToUpperInvariant(name[0]) + name[1..];
}
