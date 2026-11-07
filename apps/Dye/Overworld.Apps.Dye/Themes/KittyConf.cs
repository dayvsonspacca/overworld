using System.Globalization;
using System.Text;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

public sealed class KittyConf(string path)
{
    private const string Mark = "# Overworld's theme for kitty, written by Dye whenever a theme is applied.";

    private static readonly (string Option, string Role)[] Options =
    [
        ("foreground", "text"),
        ("background", "background"),
        ("selection_foreground", "text"),
        ("selection_background", "selected"),
        ("cursor", "accent"),
        ("cursor_text_color", "background"),
        ("url_color", "accentText"),
        ("active_border_color", "accent"),
        ("inactive_border_color", "border"),
        ("bell_border_color", "danger"),
        ("tab_bar_background", "raised"),
        ("active_tab_foreground", "text"),
        ("active_tab_background", "background"),
        ("inactive_tab_foreground", "subtle"),
        ("inactive_tab_background", "raised"),
        ("color0", "black"),
        ("color1", "red"),
        ("color2", "green"),
        ("color3", "yellow"),
        ("color4", "blue"),
        ("color5", "magenta"),
        ("color6", "cyan"),
        ("color7", "white"),
        ("color8", "brightBlack"),
        ("color9", "brightRed"),
        ("color10", "brightGreen"),
        ("color11", "brightYellow"),
        ("color12", "brightBlue"),
        ("color13", "brightMagenta"),
        ("color14", "brightCyan"),
        ("color15", "brightWhite"),
    ];

    public async Task<Result<string>> WriteAsync(string name, IReadOnlyDictionary<string, string> colors, CancellationToken cancellationToken)
    {
        if (File.Exists(path) && !(await File.ReadAllTextAsync(path, cancellationToken)).StartsWith(Mark, StringComparison.Ordinal))
        {
            return new Error("kitty.taken", $"{path} is not Overworld's; move it away so kitty wears the theme.");
        }

        var missing = Options.Select(option => option.Role).Distinct().Where(role => !colors.ContainsKey(role)).ToList();

        if (missing.Count > 0)
        {
            return new Error("kitty.missing_roles", $"'{name}' has no {string.Join(", ", missing)}, which kitty needs.");
        }

        var conf = new StringBuilder().AppendLine(Mark);

        foreach (var (option, role) in Options)
        {
            conf.AppendLine(CultureInfo.InvariantCulture, $"{option} {Opaque(colors[role])}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, conf.ToString(), cancellationToken);

        return path;
    }

    private static string Opaque(string hex) => hex.Length == 9 ? $"#{hex[3..]}" : hex;
}
