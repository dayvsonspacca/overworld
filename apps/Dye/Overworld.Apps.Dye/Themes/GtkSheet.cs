using System.Globalization;
using System.Text;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

public sealed class GtkSheet(string path)
{
    private const string Mark = "/* Overworld's theme for GTK apps, written by Dye whenever a theme is applied. */";

    private static readonly (string Variable, string Role)[] Variables =
    [
        ("window-bg-color", "background"),
        ("window-fg-color", "text"),
        ("view-bg-color", "background"),
        ("view-fg-color", "text"),
        ("headerbar-bg-color", "raised"),
        ("headerbar-fg-color", "text"),
        ("headerbar-backdrop-color", "raised"),
        ("headerbar-border-color", "divider"),
        ("sidebar-bg-color", "raised"),
        ("sidebar-fg-color", "text"),
        ("sidebar-backdrop-color", "raised"),
        ("sidebar-border-color", "divider"),
        ("secondary-sidebar-bg-color", "raised"),
        ("secondary-sidebar-fg-color", "text"),
        ("card-bg-color", "surface"),
        ("card-fg-color", "text"),
        ("dialog-bg-color", "raised"),
        ("dialog-fg-color", "text"),
        ("popover-bg-color", "surface"),
        ("popover-fg-color", "text"),
        ("thumbnail-bg-color", "surface"),
        ("thumbnail-fg-color", "text"),
        ("accent-bg-color", "accent"),
        ("accent-fg-color", "onAccent"),
        ("accent-color", "accentText"),
        ("destructive-bg-color", "danger"),
        ("destructive-color", "danger"),
        ("success-bg-color", "added"),
        ("success-color", "added"),
    ];

    public async Task<Result<string>> WriteAsync(string name, IReadOnlyDictionary<string, string> colors, CancellationToken cancellationToken)
    {
        if (File.Exists(path) && !(await File.ReadAllTextAsync(path, cancellationToken)).StartsWith(Mark, StringComparison.Ordinal))
        {
            return new Error("gtk.taken", $"{path} is not Overworld's; move it away so GTK apps wear the theme.");
        }

        var missing = Variables.Select(variable => variable.Role).Distinct().Where(role => !colors.ContainsKey(role)).ToList();

        if (missing.Count > 0)
        {
            return new Error("gtk.missing_roles", $"'{name}' has no {string.Join(", ", missing)}, which GTK apps need.");
        }

        var sheet = new StringBuilder().AppendLine(Mark).AppendLine(":root {");

        foreach (var (variable, role) in Variables)
        {
            sheet.AppendLine(CultureInfo.InvariantCulture, $"  --{variable}: {Css(colors[role])};");
        }

        sheet.AppendLine("}");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, sheet.ToString(), cancellationToken);

        return path;
    }

    private static string Css(string hex) => hex.Length == 9 ? $"#{hex[3..]}{hex[1..3]}" : hex;
}
