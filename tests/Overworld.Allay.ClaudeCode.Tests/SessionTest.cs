using System.Globalization;
using Overworld.Allay.Briefs;
using Overworld.Allay.ClaudeCode.Cli;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.ClaudeCode.Tests;

public sealed class SessionTest
{
    private const string Suggested = "[{\"type\":\"addRules\",\"rules\":[{\"toolName\":\"Bash\",\"ruleContent\":\"touch:*\"}],\"behavior\":\"allow\",\"destination\":\"session\"}]";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Prompt Ask(string text) => new(text, [], []);

    private static Task<List<Moment>> ReplyAsync(Session session, string text, Setup? setup = null) =>
        session.ReplyAsync(Ask(text), setup ?? Setup.Default, Token).ToListAsync(Token).AsTask();

    [Fact]
    public async Task RepliesWithWhatClaudeCodeStreamsKeepingTheRules()
    {
        using var claude = new FakeClaude($"read -r line\n{FakeClaude.Init}\n{FakeClaude.Writes("Olá")}\n{FakeClaude.Success}");

        var moments = await ReplyAsync(claude.Session(), "oi");

        Assert.Equal([new Notice(NoticeKind.Model, "Opus 5.5"), new Speech("Olá"), new Finished()], moments);
        Assert.Empty(ReplyRules.Broken(moments));
    }

    [Theory]
    [InlineData(Mode.Auto, "auto")]
    [InlineData(Mode.Edits, "acceptEdits")]
    [InlineData(Mode.Plan, "plan")]
    public async Task TranslatesTheMode(Mode mode, string permission)
    {
        using var claude = new FakeClaude($"{FakeClaude.SavesArguments}\nread -r line\n{FakeClaude.Success}");

        await ReplyAsync(claude.Session(), "oi", new Setup(mode));

        Assert.Equal(permission, claude.Arguments[Array.IndexOf(claude.Arguments, "--permission-mode") + 1]);
    }

    [Fact]
    public async Task AsksForTheModelAndTheHighestEffortItOffers()
    {
        using var claude = new FakeClaude($"{FakeClaude.SavesArguments}\nread -r line\n{FakeClaude.Success}");
        var capabilities = new Capabilities([], ["low", "high", "xhigh"]);

        await ReplyAsync(claude.Session(capabilities: capabilities), "oi", new Setup(Model: "claude-opus-5-5", Effort: Effort.Max));

        Assert.Equal("claude-opus-5-5", claude.Arguments[Array.IndexOf(claude.Arguments, "--model") + 1]);
        Assert.Equal("xhigh", claude.Arguments[Array.IndexOf(claude.Arguments, "--effort") + 1]);
    }

    [Fact]
    public async Task LeavesTheModelAndEffortToClaudeCodeByDefault()
    {
        using var claude = new FakeClaude($"{FakeClaude.SavesArguments}\nread -r line\n{FakeClaude.Success}");

        await ReplyAsync(claude.Session(), "oi");

        Assert.DoesNotContain("--effort", claude.Arguments);
        Assert.DoesNotContain("--model", claude.Arguments);
    }

    [Fact]
    public async Task BriefsTheSessionInOneSystemPromptWithoutNamingAnyApp()
    {
        using var claude = new FakeClaude($"{FakeClaude.SavesArguments}\nread -r line\n{FakeClaude.Success}");
        var instructions = Directory.CreateDirectory(Path.Combine(claude.Folder, "library", "commit")).FullName;
        File.WriteAllText(Path.Combine(instructions, "instructions.md"), "Use conventional commits.");
        var commit = new Skill("commit", "Commit", "Writes commits.", Path.Combine(instructions, "instructions.md"));

        await ReplyAsync(claude.Session(claude.Brief("Overworld is a desktop.", ["Never add comments"], [commit])), "oi");

        var arguments = File.ReadAllText(Path.Combine(claude.Folder, "arguments"));
        Assert.Single(claude.Arguments, line => line == "--append-system-prompt");
        Assert.Contains("Overworld is a desktop.", arguments);
        Assert.Contains("- Never add comments", arguments);
        Assert.Contains("- commit (Commit): Writes commits.", arguments);
        Assert.DoesNotContain("Crafter", arguments);
        Assert.True(arguments.IndexOf("# Context", StringComparison.Ordinal) < arguments.IndexOf("# Rules", StringComparison.Ordinal));
        Assert.Equal(Path.Combine(claude.Folder, "shelf"), claude.Arguments[Array.IndexOf(claude.Arguments, "--add-dir") + 1]);
        Assert.Equal("Use conventional commits.", File.ReadAllText(Path.Combine(claude.Folder, "shelf", "commit", "instructions.md")));
    }

    [Fact]
    public async Task GivesNoSystemPromptWithoutABriefToGive()
    {
        using var claude = new FakeClaude($"{FakeClaude.SavesArguments}\nread -r line\n{FakeClaude.Success}");

        await ReplyAsync(claude.Session(), "oi");

        Assert.DoesNotContain("--append-system-prompt", claude.Arguments);
        Assert.DoesNotContain("--mcp-config", claude.Arguments);
        Assert.Contains("--disable-slash-commands", claude.Arguments);
    }

    [Fact]
    public async Task CallsASkillAndPointsAtFilesInTheMessage()
    {
        using var claude = new FakeClaude("read -r line\nprintf '%s' \"$line\" > \"$(dirname \"$0\")/asked\"\n" + FakeClaude.Success);
        var instructions = Directory.CreateDirectory(Path.Combine(claude.Folder, "library", "commit")).FullName;
        File.WriteAllText(Path.Combine(instructions, "instructions.md"), "Use conventional commits.");
        var commit = new Skill("commit", "Commit", "Writes commits.", Path.Combine(instructions, "instructions.md"));
        var session = claude.Session(claude.Brief(skills: [commit]));

        await session.ReplyAsync(new Prompt("commit it", [new FileMention("src/App.cs")], ["commit"]), Setup.Default, Token).ToListAsync(Token);

        var asked = File.ReadAllText(Path.Combine(claude.Folder, "asked"));
        Assert.Contains("<skill id=\\\"commit\\\"", asked);
        Assert.Contains("Use conventional commits.", asked);
        Assert.Contains("- src/App.cs", asked);
    }

    [Fact]
    public async Task PassesARebriefOnceAsADifferenceKeepingTheSystemPrompt()
    {
        using var claude = new FakeClaude(
            "printf '%s\\n' \"$@\" >> \"$(dirname \"$0\")/arguments\"\nread -r line\nprintf '%s\\n' \"$line\" >> \"$(dirname \"$0\")/asked\"\n" + FakeClaude.Success);
        var brief = claude.Brief(rules: ["Never add comments"]);
        var session = claude.Session(brief);

        await ReplyAsync(session, "first");
        session.Rebrief(brief with { Rules = ["Never add comments", "Prefer short names"] });
        await ReplyAsync(session, "second");
        await ReplyAsync(session, "third");

        var asked = File.ReadAllLines(Path.Combine(claude.Folder, "asked"));
        Assert.DoesNotContain("brief-update", asked[0]);
        Assert.Contains("New rule: Prefer short names", asked[1]);
        Assert.DoesNotContain("brief-update", asked[2]);
        Assert.DoesNotContain("- Prefer short names", File.ReadAllText(Path.Combine(claude.Folder, "arguments")));
    }

    [Fact]
    public async Task ConnectsTheToolServersAndLetsTheirFreeToolsRunUnasked()
    {
        using var claude = new FakeClaude($"{FakeClaude.SavesArguments}\nread -r line\n{FakeClaude.Success}");
        ToolServer[] servers = [new("crafter", new Uri("http://127.0.0.1:4100/mcp"), "t0ken", ["list_todos", "add_todo"], TimeSpan.FromMinutes(30))];

        await ReplyAsync(claude.Session(claude.Brief(tools: servers)), "oi");

        Assert.Equal(
            """{"mcpServers":{"crafter":{"type":"http","url":"http://127.0.0.1:4100/mcp","headers":{"Authorization":"Bearer t0ken"},"timeout":1800000}}}""",
            claude.Arguments[Array.IndexOf(claude.Arguments, "--mcp-config") + 1]);
        Assert.Equal(["mcp__crafter__list_todos", "mcp__crafter__add_todo"], claude.Arguments.SkipWhile(each => each != "--allowedTools").Skip(1).Take(2));
    }

    [Fact]
    public async Task StartsClaudeCodeWithoutItsMemoryOrInstructionFiles()
    {
        using var claude = new FakeClaude($"env > \"$(dirname \"$0\")/environment\"\nread -r line\n{FakeClaude.Success}");

        await ReplyAsync(claude.Session(), "oi");

        var environment = File.ReadAllLines(Path.Combine(claude.Folder, "environment"));
        Assert.Contains("CLAUDE_CODE_DISABLE_CLAUDE_MDS=1", environment);
        Assert.Contains("CLAUDE_CODE_DISABLE_AUTO_MEMORY=1", environment);
    }

    [Fact]
    public async Task ResumesTheSessionOnTheNextPromptAndOneItWasGiven()
    {
        using var claude = new FakeClaude(
            $"""
            read -r line
            if [[ " $* " == *" --resume s-"* ]]; then text=resumed; else text=fresh; fi
            {FakeClaude.Init}
            {FakeClaude.Writes("$text")}
            {FakeClaude.Success}
            """);
        var session = claude.Session();

        Assert.Contains(new Speech("fresh"), await ReplyAsync(session, "oi"));
        Assert.Equal("s-1", session.Id);
        Assert.Contains(new Speech("resumed"), await ReplyAsync(session, "e agora?"));
        Assert.Contains(new Speech("resumed"), await ReplyAsync(claude.Session(resume: "s-0"), "oi"));
    }

    [Fact]
    public async Task StopsWhenAsked()
    {
        using var claude = new FakeClaude($"read -r line\n{FakeClaude.Init}\nsleep 30");
        using var asked = CancellationTokenSource.CreateLinkedTokenSource(Token);
        List<Moment> moments = [];

        await foreach (var moment in claude.Session().ReplyAsync(Ask("oi"), Setup.Default, asked.Token))
        {
            moments.Add(moment);

            if (moment is Notice)
            {
                await asked.CancelAsync();
            }
        }

        Assert.Equal([new Notice(NoticeKind.Model, "Opus 5.5"), new Stopped()], moments);
    }

    [Fact]
    public async Task FailsWhenClaudeCodeIsMissingOrComplains()
    {
        var missing = new Session("/nonexistent/claude", () => Capabilities.Unknown, Path.GetTempPath(), new Brief(Path.GetTempPath(), null, [], [], []), null);
        using var complaining = new FakeClaude("read -r line\necho 'Not logged in' >&2\nexit 1");
        using var silent = new FakeClaude("read -r line\nexit 3");

        Assert.IsType<Failed>(Assert.Single(await ReplyAsync(missing, "oi")));
        Assert.Equal([new Failed("Not logged in")], await ReplyAsync(complaining.Session(), "oi"));
        Assert.Equal([new Failed("Claude Code exited with code 3 without a reply.")], await ReplyAsync(silent.Session(), "oi"));
    }

    [Theory]
    [InlineData(true, "allowed")]
    [InlineData(false, "denied")]
    public async Task AsksPermissionBeforeActing(bool allow, string verdict)
    {
        using var claude = new FakeClaude("read -r line\n" + Asks("r-1") + "\n" + FakeClaude.Writes("$verdict") + "\n" + FakeClaude.Success);
        var session = claude.Session();

        var moments = await DecidedAsync(session, allow ? new AllowOnce() : new Deny());

        Assert.Equal(new Permission("r-1", ActKind.Command, "touch x", "Create x", []), moments[0]);
        Assert.Contains(new Speech(verdict), moments);
    }

    [Fact]
    public async Task RemembersWhatClaudeCodeSuggestedWhenAllowedAlways()
    {
        using var claude = new FakeClaude(
            "read -r line\n"
            + "if [[ \" $* \" == *\" --allowedTools Bash(touch:*) \"* ]]; then verdict=remembered; else\n"
            + "echo '{\"type\":\"control_request\",\"request_id\":\"r-1\",\"request\":{\"subtype\":\"can_use_tool\",\"tool_name\":\"Bash\",\"input\":{\"command\":\"touch x\"},\"permission_suggestions\":" + Suggested + "}}'\n"
            + "read -r answer\n"
            + "if [[ \"$answer\" == *'\"updatedPermissions\":[{\"type\":\"addRules\"'* ]]; then verdict=always; else verdict=once; fi\n"
            + "fi\n"
            + FakeClaude.Writes("$verdict") + "\n" + FakeClaude.Success);
        var session = claude.Session();

        var first = await DecidedAsync(session, new AllowAlways());
        var second = await DecidedAsync(session, new AllowOnce());

        Assert.Equal([new Allowance("Bash(touch:*)", Reach.Conversation)], first.OfType<Permission>().Single().Remembers);
        Assert.Contains(new Speech("always"), first);
        Assert.Contains(new Speech("remembered"), second);
    }

    [Fact]
    public async Task TellsClaudeCodeWhyItWasDenied()
    {
        using var claude = new FakeClaude(
            "read -r line\n" + Asks("r-1") + "\n"
            + "if [[ \"$answer\" == *'\"message\":\"The user denied this and said: use mkdir\"'* ]]; then why=told; else why=silent; fi\n"
            + FakeClaude.Writes("$why") + "\n" + FakeClaude.Success);

        Assert.Contains(new Speech("told"), await DecidedAsync(claude.Session(), new Deny("  use mkdir ")));
    }

    [Fact]
    public async Task HandsTheAnswersBackWithTheQuestions()
    {
        using var claude = new FakeClaude(
            "read -r line\n"
            + "echo '{\"type\":\"control_request\",\"request_id\":\"r-1\",\"request\":{\"subtype\":\"can_use_tool\",\"tool_name\":\"AskUserQuestion\",\"input\":{\"questions\":[{\"question\":\"Which?\",\"header\":\"Pick\",\"multiSelect\":true,\"options\":[{\"label\":\"A\"},{\"label\":\"B\"}]}]}}}'\n"
            + "read -r answer\n"
            + "if [[ \"$answer\" == *'\"behavior\":\"allow\"'*'\"answers\":{\"Which?\":\"A, B, and C\"}'* ]]; then heard=answered; else heard=lost; fi\n"
            + FakeClaude.Writes("$heard") + "\n" + FakeClaude.Success);
        var session = claude.Session();
        List<Moment> moments = [];

        await foreach (var moment in session.ReplyAsync(Ask("pergunta"), Setup.Default, Token))
        {
            moments.Add(moment);

            if (moment is Questions asked)
            {
                session.Decide(asked.Id, new Answers([new QuestionAnswer(asked.Asked[0], [.. asked.Asked[0].Options], "and C")]));
            }
        }

        Assert.IsType<Questions>(moments[0]);
        Assert.Contains(new Speech("answered"), moments);
        Assert.Empty(ReplyRules.Broken(moments));
    }

    [Fact]
    public async Task LetsGoOfARequestClaudeCodeWithdraws()
    {
        using var claude = new FakeClaude(
            "read -r line\n"
            + "echo '{\"type\":\"control_request\",\"request_id\":\"r-1\",\"request\":{\"subtype\":\"can_use_tool\",\"tool_name\":\"Bash\",\"input\":{\"command\":\"touch x\"}}}'\n"
            + "echo '{\"type\":\"control_cancel_request\",\"request_id\":\"r-1\"}'\n"
            + FakeClaude.Writes("moved on") + "\n" + FakeClaude.Success);

        var moments = await ReplyAsync(claude.Session(), "toca");

        Assert.Equal("r-1", Assert.IsType<Permission>(moments[0]).Id);
        Assert.Equal([new Withdrawn("r-1"), new Speech("moved on"), new Finished()], moments[1..]);
        Assert.Empty(ReplyRules.Broken(moments));
    }

    [Fact]
    public async Task EndsTheReplyWhileAJobRunsAndWakesWhenItSettles()
    {
        using var claude = new FakeClaude(
            "read -r line\n"
            + FakeClaude.Init + "\n"
            + Job("b-1") + "\n"
            + FakeClaude.Writes("started") + "\n" + FakeClaude.Success + "\n"
            + "sleep 0.3\n"
            + "echo '{\"type\":\"system\",\"subtype\":\"task_notification\",\"task_id\":\"b-1\",\"status\":\"completed\"}'\n"
            + FakeClaude.Init + "\n" + FakeClaude.Writes("it finished") + "\n" + FakeClaude.Success + "\n"
            + "read -r line");
        var session = claude.Session();
        var woken = new TaskCompletionSource<IAsyncEnumerable<Moment>>(TaskCreationOptions.RunContinuationsAsynchronously);
        List<IReadOnlyList<Job>> changes = [];
        session.Woke += (_, moments) => woken.TrySetResult(moments);
        session.JobsChanged += (_, jobs) => changes.Add(jobs);

        var moments = await ReplyAsync(session, "start the server");
        var running = session.Jobs;
        var later = await (await woken.Task.WaitAsync(Token)).ToListAsync(Token);
        session.End();

        Assert.Equal([new Notice(NoticeKind.Model, "Opus 5.5"), new Speech("started"), new Finished()], moments);
        Assert.Equal([new Job("b-1", JobKind.Command, "Dev server")], running);
        Assert.Equal([new Notice(NoticeKind.Model, "Opus 5.5"), new Speech("it finished"), new Finished()], later);
        Assert.Empty(changes[^1]);
    }

    [Fact]
    public async Task EndingStopsClaudeCodeItStarted()
    {
        using var claude = new FakeClaude("echo $$ > \"$(dirname \"$0\")/pid\"\nread -r line\n" + Job("b-1") + "\n" + FakeClaude.Success + "\nsleep 30");
        var session = claude.Session();
        await ReplyAsync(session, "hello");
        var started = int.Parse(File.ReadAllText(Path.Combine(claude.Folder, "pid")).Trim(), CultureInfo.InvariantCulture);
        using var process = System.Diagnostics.Process.GetProcessById(started);

        session.End();

        await process.WaitForExitAsync(Token).WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.True(process.HasExited);
    }

    private static string Job(string id) =>
        "echo '{\"type\":\"system\",\"subtype\":\"task_started\",\"task_id\":\"" + id + "\",\"description\":\"Dev server\",\"task_type\":\"local_bash\"}'";

    private static string Asks(string request) =>
        "echo '{\"type\":\"control_request\",\"request_id\":\"" + request
        + "\",\"request\":{\"subtype\":\"can_use_tool\",\"tool_name\":\"Bash\",\"input\":{\"command\":\"touch x\"},\"description\":\"Create x\"}}'\n"
        + "read -r answer\n"
        + "if [[ \"$answer\" == *'\"behavior\":\"allow\"'*'\"updatedInput\":{\"command\":\"touch x\"}'* ]]; then verdict=allowed; else verdict=denied; fi";

    private static async Task<List<Moment>> DecidedAsync(Session session, Decision decision)
    {
        List<Moment> moments = [];

        await foreach (var moment in session.ReplyAsync(Ask("toca"), Setup.Default, Token))
        {
            moments.Add(moment);

            if (moment is Permission permission)
            {
                session.Decide(permission.Id, decision);
            }
        }

        return moments;
    }
}
