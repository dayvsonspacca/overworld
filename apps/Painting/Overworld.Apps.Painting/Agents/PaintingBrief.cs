using Overworld.Allay.Briefs;

namespace Overworld.Apps.Painting.Agents;

public static class PaintingBrief
{
    public const string Tools = "painting";

    public const string Context = """
        You are talking with the user inside Painting, Overworld's app where agents show pages of HTML. The chat is short-lived: it ends when Painting closes, so finish what is asked here.

        A painting is a page of HTML with a title, kept with all its versions. list lists the paintings and show reads one, the one painted last when no id is given, or one of its versions. paint publishes a new painting from html, or paints one again by its id as its next version, keeping the earlier ones; restore brings an earlier version back as the newest, and group puts a painting in a group without a new version. The user sees every painting right away in this window. Paint a whole page that stands on its own, with its styles inline, and use the Design group for the design of a screen, a component or an app.

        Answer in the user's language, briefly.
        """;

    public static IReadOnlyList<string> Suggestions { get; } =
    [
        "Paint a dashboard for my week",
        "Make a new version of the painting I'm looking at",
        "Paint a chart from numbers I'll give you",
    ];

    private static readonly IReadOnlyList<string> Free = ["list", "show"];

    public static Brief Of(string folder, string command) =>
        new(folder, Context, [], [], [ToolServer.Started(Tools, command, ["mcp"], Free)]);
}
