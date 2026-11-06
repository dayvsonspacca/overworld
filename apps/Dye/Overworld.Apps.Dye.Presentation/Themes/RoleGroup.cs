using Overworld.Apps.Dye.Themes;

namespace Overworld.Apps.Dye.Presentation.Themes;

public sealed record RoleGroup(string Title, IReadOnlyList<RoleSwatch> Swatches)
{
    private static readonly (string Title, string[] Roles)[] Known =
    [
        ("Surfaces", ["background", "raised", "surface", "divider", "border"]),
        ("Text", ["faint", "subtle", "textSoft", "text"]),
        ("Accent", ["accentDeep", "accentPressed", "accent", "accentHover", "accentText", "accentLight", "onAccent", "selected"]),
        ("States", ["danger", "added"]),
        ("Tints", ["scrim", "hoverTint", "edge", "activeTint", "activeHoverTint"]),
    ];

    public static IReadOnlyList<RoleGroup> Of(Theme theme)
    {
        var placed = Known.SelectMany(group => group.Roles).ToHashSet();
        var others = theme.Colors.Keys.Where(role => !placed.Contains(role)).ToArray();

        return
        [
            .. Known
                .Append((Title: "Other", Roles: others))
                .Select(group => new RoleGroup(
                    group.Title,
                    [.. group.Roles.Where(theme.Colors.ContainsKey).Select(role => new RoleSwatch(role, theme.Colors[role]))]))
                .Where(group => group.Swatches.Count > 0),
        ];
    }
}
