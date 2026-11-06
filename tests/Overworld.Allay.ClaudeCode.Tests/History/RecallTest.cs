using System.Text.Json;
using Overworld.Allay.ClaudeCode.History;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.ClaudeCode.Tests.History;

public sealed class RecallTest : IDisposable
{
    private const string Project = "/home/hero/Projetos/me/overworld";

    private readonly string _home = Directory.CreateTempSubdirectory("overworld-claude-home-").FullName;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string Sessions => Path.Combine(_home, "projects", "-home-hero-Projetos-me-overworld");

    public void Dispose() => Directory.Delete(_home, recursive: true);

    private static Prompt Ask(string text) => new(text, [], []);

    private static Reply Said(string text) => new([new Speech(text)], new Finished());

    [Fact]
    public async Task HasNoHistoryForAFolderItNeverSaw()
    {
        var history = await new Recall(_home).ListAsync(Project, 5, Token);

        Assert.Empty(history.Latest);
        Assert.Equal(0, history.Total);
    }

    [Fact]
    public async Task ListsSessionsByTheirFirstPromptNewestFirst()
    {
        Write("old", DateTime.UtcNow.AddDays(-2), User("Explain the crew"), Assistant("It hires agents."));
        Write("new", DateTime.UtcNow, Queue(), User("Fix reconnect after TimedOut and cover it with a test\nplease"));

        var sessions = (await new Recall(_home).ListAsync(Project, 5, Token)).Latest;

        Assert.Equal(["new", "old"], sessions.Select(session => session.Id));
        Assert.Equal(["Fix reconnect after TimedOut and cover…", "Explain the crew"], sessions.Select(session => session.Title));
    }

    [Fact]
    public async Task RemembersTheLastPromptOfALongerSession()
    {
        Write("long", DateTime.UtcNow, User("Fix it"), Assistant("Done."), User("Now test it"), Assistant("Tested."));
        Write("short", DateTime.UtcNow.AddMinutes(-1), User("Explain the crew"));

        var sessions = (await new Recall(_home).ListAsync(Project, 5, Token)).Latest;

        Assert.Equal(Ask("Now test it"), sessions[0].LastPrompt);
        Assert.Null(sessions[1].LastPrompt);
    }

    [Fact]
    public async Task KeepsTheLatestAndCountsTheRest()
    {
        for (var at = 0; at < 8; at++)
        {
            Write($"s-{at}", DateTime.UtcNow.AddMinutes(at), User($"Prompt {at}"));
        }

        Write("empty", DateTime.UtcNow.AddMinutes(-5), Queue());

        var history = await new Recall(_home).ListAsync(Project, 5, Token);

        Assert.Equal(["s-7", "s-6", "s-5", "s-4", "s-3"], history.Latest.Select(session => session.Id));
        Assert.Equal(8, history.Total);
    }

    [Fact]
    public async Task ReplaysPromptsAndTheSpeechOfTheReplies()
    {
        Write(
            "s-1",
            DateTime.UtcNow,
            Assistant("before any prompt"),
            User("Fix it"),
            Assistant("Looking."),
            ToolResult(),
            Assistant("Fixed."),
            Sidechain("Agent task"),
            User("Thanks"),
            Assistant("Anytime."));

        var turns = await new Recall(_home).ReplayAsync(Project, "s-1", Token);

        Assert.Equal([Ask("Fix it"), Said("Looking.\n\nFixed."), Ask("Thanks"), Said("Anytime.")], turns);
    }

    [Fact]
    public async Task LeavesOutWhatTheAppAndClaudeCodeAddedToAPrompt()
    {
        Write(
            "s-1",
            DateTime.UtcNow,
            User("<system-reminder>Be brief.</system-reminder>\nAnd now?"),
            User("<local-command-stdout>Compacted</local-command-stdout>"),
            User("<brief-update>\n- New rule: Prefer short names\n</brief-update>\n\n<skill id=\"commit\" name=\"Commit\" folder=\"/shelf/commit\">\nUse conventional commits.\n</skill>\n\ncommit it"),
            User("<crafter-skill id=\"commit\" name=\"Commit\" folder=\"/shelf/commit\">\nUse conventional commits.\n</crafter-skill>\n\n/commit it"));

        var turns = await new Recall(_home).ReplayAsync(Project, "s-1", Token);

        Assert.Equal([Ask("And now?"), Ask("commit it"), Ask("/commit it")], turns);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("../escape")]
    public async Task ReplaysNothingForASessionItCannotFind(string session)
    {
        Assert.Empty(await new Recall(_home).ReplayAsync(Project, session, Token));
    }

    private static string User(object content) =>
        JsonSerializer.Serialize(new { type = "user", isSidechain = false, message = new { role = "user", content } });

    private static string Sidechain(string text) =>
        JsonSerializer.Serialize(new { type = "user", isSidechain = true, message = new { role = "user", content = text } });

    private static string ToolResult() =>
        JsonSerializer.Serialize(new { type = "user", message = new { role = "user", content = new[] { new { type = "tool_result", content = "ok" } } } });

    private static string Assistant(string text) =>
        JsonSerializer.Serialize(new { type = "assistant", message = new { role = "assistant", content = new[] { new { type = "text", text } } } });

    private static string Queue() => JsonSerializer.Serialize(new { type = "queue-operation", operation = "enqueue" });

    private void Write(string session, DateTime touched, params string[] lines)
    {
        Directory.CreateDirectory(Sessions);
        var file = Path.Combine(Sessions, session + ".jsonl");
        File.WriteAllLines(file, lines);
        File.SetLastWriteTimeUtc(file, touched);
    }
}
