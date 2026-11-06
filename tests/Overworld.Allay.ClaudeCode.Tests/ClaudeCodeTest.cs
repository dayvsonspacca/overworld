using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.ClaudeCode.Tests.Cli;
using Overworld.Allay.Conversations;
using Overworld.Allay.Settings;

namespace Overworld.Allay.ClaudeCode.Tests;

public sealed class ClaudeCodeTest
{
    private const string Version = "echo '2.1.317 (Claude Code)'";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static ClaudeCode Agent(FakeClaude claude) => new(Path.Combine(claude.Folder, "shelves"), claude.Executable);

    private static string Script(string status) =>
        $"""
        if [[ "$1" == "--version" ]]; then {Version}; exit 0; fi
        if [[ "$1" == "auth" ]]; then {status}; fi
        """;

    [Fact]
    public void IsKnownAsClaudeCode()
    {
        var agent = new ClaudeCode(Path.GetTempPath());

        Assert.Equal(new AgentId("claude-code"), agent.Id);
        Assert.Equal("Claude Code", agent.Name);
        Assert.Equal(["CLAUDE.md", Path.Combine(".claude", "CLAUDE.md")], agent.InstructionFiles);
    }

    [Theory]
    [InlineData("""echo '{"loggedIn":true,"authMethod":"claude.ai","subscriptionType":"team"}'""", "Version 2.1.317 · claude.ai · team")]
    [InlineData("echo '{\"loggedIn\":true}'", "Version 2.1.317")]
    public async Task IsReadyWhenInstalledAndSignedIn(string status, string detail)
    {
        using var claude = new FakeClaude(Script(status));

        Assert.Equal(new Ready("Ready", detail), await Agent(claude).CheckAsync(Token));
    }

    [Theory]
    [InlineData("""echo '{"loggedIn":false,"authMethod":"none"}'""")]
    [InlineData("""echo '{"loggedIn":false,"authMethod":"none"}'; exit 1""")]
    public async Task AsksToSignInWhenSignedOut(string status)
    {
        using var claude = new FakeClaude(Script(status));

        Assert.Equal(new Blocked("Signed out", "Sign in from a terminal.", ClaudeCode.SignIn), await Agent(claude).CheckAsync(Token));
    }

    [Theory]
    [InlineData("echo 'unknown command' >&2; exit 1", "unknown command")]
    [InlineData("echo 'not json'", "`claude auth status` did not say whether it is signed in.")]
    [InlineData("echo '{\"authMethod\":\"claude.ai\"}'", "`claude auth status` did not say whether it is signed in.")]
    [InlineData("echo '[]'", "`claude auth status` did not say whether it is signed in.")]
    public async Task IsBrokenWhenItCannotTellWhetherItIsSignedIn(string status, string detail)
    {
        using var claude = new FakeClaude(Script(status));

        Assert.Equal(new Blocked("Not working", detail), await Agent(claude).CheckAsync(Token));
    }

    [Fact]
    public async Task AsksToInstallWhenMissing()
    {
        var readiness = await new ClaudeCode(Path.GetTempPath(), "/nonexistent/claude").CheckAsync(Token);

        Assert.Equal(new Blocked("Not installed", "Install it from a terminal.", ClaudeCode.Install), readiness);
    }

    [Theory]
    [InlineData("echo 'libc mismatch' >&2\nexit 2", "libc mismatch")]
    [InlineData("exit 2", "`claude --version` exited with code 2.")]
    [InlineData("echo '{\"type\":\"system\"}'", "`claude --version` did not answer with a version.")]
    public async Task IsBrokenWithWhatWentWrong(string script, string detail)
    {
        using var claude = new FakeClaude(script);

        Assert.Equal(new Blocked("Not working", detail), await Agent(claude).CheckAsync(Token));
    }

    [Fact]
    public async Task OffersTheModelsTheInstalledClaudeCodeKnows()
    {
        using var claude = new FakeClaude(
            "if [[ \"$1\" == \"--help\" ]]; then cat \"$(dirname \"$0\")/help.txt\"; exit 0; fi\n"
            + "if [[ \"$*\" == *--input-format* ]]; then read -r line; cat \"$(dirname \"$0\")/introduction.json\"; sleep 5; fi");
        File.WriteAllText(Path.Combine(claude.Folder, "help.txt"), CapabilitiesTest.Help);
        File.WriteAllText(Path.Combine(claude.Folder, "introduction.json"), CapabilitiesTest.Introduction + "\n");

        var models = await Agent(claude).ModelsAsync(Token);

        Assert.Equal(["opus", "claude-sonnet-4-6", "claude-next"], models.Select(model => model.Id));
    }

    [Fact]
    public async Task FallsBackToTheAliasesWhenClaudeCodeDoesNotIntroduceItself()
    {
        using var claude = new FakeClaude(
            "if [[ \"$1\" == \"--help\" ]]; then cat \"$(dirname \"$0\")/help.txt\"; exit 0; fi\n"
            + "if [[ \"$*\" == *--input-format* ]]; then exit 1; fi");
        File.WriteAllText(Path.Combine(claude.Folder, "help.txt"), CapabilitiesTest.Help);

        Assert.Equal(["fable", "opus", "sonnet"], (await Agent(claude).ModelsAsync(Token)).Select(model => model.Id));
    }

    [Fact]
    public async Task OffersNoModelsWhenItCannotAsk()
    {
        Assert.Empty(await new ClaudeCode(Path.GetTempPath(), "/nonexistent/claude").ModelsAsync(Token));
    }

    [Fact]
    public async Task BeginsASessionInTheBriefsFolder()
    {
        using var claude = new FakeClaude($"read -r line\n{FakeClaude.Writes("$(pwd)")}\n{FakeClaude.Success}");
        var session = Agent(claude).Begin(new Brief(claude.Folder, null, [], [], []), resume: "s-0");

        var moments = await session.ReplyAsync(new Prompt("oi", [], []), Setup.Default, Token).ToListAsync(Token);

        Assert.Contains(new Speech(claude.Folder), moments);
        Assert.Equal("s-1", session.Id);
    }
}
