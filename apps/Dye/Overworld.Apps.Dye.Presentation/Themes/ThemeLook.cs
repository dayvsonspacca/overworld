using Overworld.Apps.Dye.Themes;

namespace Overworld.Apps.Dye.Presentation.Themes;

public sealed record ThemeLook(
    Theme Theme,
    Variant Variant,
    string Background,
    string Raised,
    string Surface,
    string Divider,
    string Text,
    string TextSoft,
    string Subtle,
    string Accent,
    string OnAccent,
    string Selected,
    string Danger)
{
    private const string Unset = "#00000000";

    public string Name => Theme.Name;

    public bool Active => Theme.Active;

    public string? Wearing => Theme.Worn?.ToString().ToLowerInvariant();

    public static ThemeLook Of(Theme theme, Variant variant)
    {
        string Color(string role) => theme.Colors(variant).GetValueOrDefault(role, Unset);

        return new ThemeLook(
            theme,
            variant,
            Color("background"),
            Color("raised"),
            Color("surface"),
            Color("divider"),
            Color("text"),
            Color("textSoft"),
            Color("subtle"),
            Color("accent"),
            Color("onAccent"),
            Color("selected"),
            Color("danger"));
    }
}
