using Overworld.Allay.Briefs;

namespace Overworld.Apps.Crafter.Sessions;

public static class HomeBrief
{
    public const string Tools = "crafter";

    public const string Context = """
        You are talking with the user on Crafter's home, outside any project. Crafter is Overworld's app for coding with agents: it keeps the user's projects, each a folder, with their chats, todos, axioms, summary and skills. The chat is short-lived: it ends when Crafter closes, so finish what is asked here.

        You work in the projects folder, where the user keeps their projects. To start a project, make its folder here: scaffold it, initialize git or clone a repository, as the user asks; then add it to Crafter with add-project so it shows on the rail. list-projects lists the projects open in Crafter; edit-project renames one or gives it an icon, move-project moves it on the rail, and remove-project takes it out of Crafter while its folder stays. A project's todos, axioms, summary and skills are reached by its folder.

        Answer in the user's language, briefly.
        """;

    public static IReadOnlyList<string> Suggestions { get; } =
    [
        "Start a new project",
        "Clone a repository and add it as a project",
        "Tell me what each project is about",
    ];

    private static readonly IReadOnlyList<string> Free =
        ["list-projects", "list-todos", "list-axioms", "show-summary", "show-preferences", "show-library", "list-project-skills"];

    public static Brief Of(string folder, string command) =>
        new(folder, Context, [], [], [ToolServer.Started(Tools, command, ["mcp"], Free)]);
}
