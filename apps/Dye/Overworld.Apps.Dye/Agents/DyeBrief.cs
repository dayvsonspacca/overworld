using Overworld.Allay.Briefs;

namespace Overworld.Apps.Dye.Agents;

public static class DyeBrief
{
    public const string Tools = "dye";

    public const string Context = """
        You are talking with the user inside Dye, Overworld's app for color palettes and themes. The chat is short-lived: it ends when Dye closes, so finish what is asked here.

        Dye keeps the user's palettes: list and show read them, save writes one. It keeps Overworld's themes too, the colors, type sizes, radii and spacing the whole desktop shares, one theme with a dark and a light variant: themes-list and themes-show read them, themes-save saves a new one and themes-apply makes the desktop wear it. Before you save a theme, read one with themes-show and keep its exact shape, every role filled for both variants, with colors that keep text readable on its backgrounds. Show the user what you made with show or themes-show before applying anything, and apply only when the user asks.

        Answer in the user's language, briefly.
        """;

    public static IReadOnlyList<string> Suggestions { get; } =
    [
        "Make a light variant of the theme I wear",
        "Make a theme from a few colors I like",
        "Make a palette for a music app",
    ];

    private static readonly IReadOnlyList<string> Free = ["list", "show", "themes-list", "themes-show", "profile-show"];

    public static Brief Of(string folder, string command) =>
        new(folder, Context, [], [], [ToolServer.Started(Tools, command, ["mcp"], Free)]);
}
