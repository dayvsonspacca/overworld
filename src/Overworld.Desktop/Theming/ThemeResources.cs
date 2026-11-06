using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Overworld.Desktop.Theming;

public sealed class ThemeResources : ResourceDictionary
{
    private readonly string _path;

    public ThemeResources()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "overworld", "theme.json"))
    {
    }

    public ThemeResources(string path)
    {
        _path = path;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"There is no theme at {path}; run just seed to install one.", path);
        }

        var theme = Read();
        var colors = Colors(theme);
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

        Paint(colors);
    }

    public void Reload()
    {
        IReadOnlyList<(string Name, Color Color)> colors;

        try
        {
            colors = Colors(Read());
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or FormatException or InvalidOperationException)
        {
            return;
        }

        Paint(colors);
    }

    private JsonNode Read() =>
        JsonNode.Parse(File.ReadAllText(_path)) ?? throw new InvalidDataException($"{_path} holds null, not a theme.");

    private List<(string Name, Color Color)> Colors(JsonNode theme) =>
        [.. Section(theme, "colors").Select(color => (Pascal(color.Key), Color.Parse(color.Value!.GetValue<string>())))];

    private void Paint(IReadOnlyList<(string Name, Color Color)> colors)
    {
        foreach (var (name, color) in colors)
        {
            this[$"{name}Color"] = color;
            this[$"{name}Brush"] = new SolidColorBrush(color);
        }

        var accent = (Color)this["AccentColor"]!;

        this["FocusGlow"] = new BoxShadows(new BoxShadow { Spread = 3, Color = Color.FromArgb(0x38, accent.R, accent.G, accent.B) });
    }

    private JsonObject Section(JsonNode node, string name) =>
        node[name] as JsonObject ?? throw new InvalidDataException($"{_path} has no '{name}'.");

    private static string Pascal(string name) => char.ToUpperInvariant(name[0]) + name[1..];
}
