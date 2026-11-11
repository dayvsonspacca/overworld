using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.ClaudeCode.Cli;
using Overworld.Allay.ClaudeCode.History;
using Overworld.Allay.ClaudeCode.Protocol;
using Overworld.Allay.Conversations;
using Overworld.Allay.Metering;
using Overworld.Allay.Settings;
using Overworld.Bedrock.Results;
using static Overworld.Allay.ClaudeCode.Protocol.JsonFields;

namespace Overworld.Allay.ClaudeCode;

[SuppressMessage(
    "Design",
    "CA1001",
    Justification = "The learning gate never hands out its wait handle, so it holds nothing that needs disposing.")]
public sealed class ClaudeCode(string shelves, string executable = "claude", string? home = null) : IAgent, IKeepsHistory, IMetered
{
    public const string Install = "curl -fsSL https://claude.ai/install.sh | bash";

    public const string SignIn = "claude auth login";

    private const string Broken = "Not working";

    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(20);

    private readonly Recall _history = new(home ?? Recall.Home);

    private readonly SemaphoreSlim _learning = new(1, 1);

    private Capabilities? _capabilities;

    public AgentId Id { get; } = new("claude-code");

    public string Name => "Claude Code";

    public string Summary => "Anthropic's coding agent";

    public string Color => "#D97757";

    public IReadOnlyList<string> InstructionFiles { get; } = ["CLAUDE.md", Path.Combine(".claude", "CLAUDE.md")];

    public async Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) =>
        (await CapabilitiesAsync(cancellationToken)).Models;

    public async Task<Readiness> CheckAsync(CancellationToken cancellationToken)
    {
        var version = await AskAsync(["--version"], cancellationToken);

        if (version is null)
        {
            return new Blocked("Not installed", "Install it from a terminal.", Install);
        }

        if (version.Code != 0)
        {
            return new Blocked(Broken, Said(version, $"`claude --version` exited with code {version.Code}."));
        }

        var installed = version.Output.Trim().Split(' ', 2)[0];

        if (installed.Length == 0 || !char.IsAsciiDigit(installed[0]))
        {
            return new Blocked(Broken, "`claude --version` did not answer with a version.");
        }

        var status = await AskAsync(["auth", "status", "--json"], cancellationToken);

        return (status is null ? null : Account(status.Output)) switch
        {
            { LoggedIn: true, Label: { } label } => new Ready("Ready", $"Version {installed} · {label}"),
            { LoggedIn: true } => new Ready("Ready", $"Version {installed}"),
            { LoggedIn: false } => new Blocked("Signed out", "Sign in from a terminal.", SignIn),
            _ => new Blocked(Broken, status is null ? "`claude auth status` did not start." : Said(status, "`claude auth status` did not say whether it is signed in.")),
        };
    }

    public ISession Begin(Brief brief, string? resume = null) =>
        new Session(executable, () => _capabilities ?? Capabilities.Unknown, SkillShelf.FolderFor(shelves, brief.Folder), brief, resume);

    public Task<Recollection> ListAsync(string folder, int latest, CancellationToken cancellationToken) =>
        _history.ListAsync(folder, latest, cancellationToken);

    public Task<IReadOnlyList<Turn>> ReplayAsync(string folder, string session, CancellationToken cancellationToken) =>
        _history.ReplayAsync(folder, session, cancellationToken);

    public async Task<Result<Usage>> UsageAsync(CancellationToken cancellationToken)
    {
        var answer = await AskAsync(["-p", "/usage", "--no-session-persistence", "--output-format", "stream-json", "--verbose"], cancellationToken);

        if (answer is null)
        {
            return Unmeasured("`claude` did not start.");
        }

        if (answer.Code != 0)
        {
            return Unmeasured(Said(answer, $"`claude -p /usage` exited with code {answer.Code}."));
        }

        return UsageReport.Read(answer.Output) is { } usage ? usage : Unmeasured("`claude -p /usage` did not report it.");
    }

    private async Task<Capabilities> CapabilitiesAsync(CancellationToken cancellationToken)
    {
        if (_capabilities is { } known)
        {
            return known;
        }

        await _learning.WaitAsync(cancellationToken);

        try
        {
            if (_capabilities is { } learned)
            {
                return learned;
            }

            if (await AskAsync(["--help"], cancellationToken) is not { Code: 0 } help)
            {
                return Capabilities.Unknown;
            }

            return _capabilities = Capabilities.Parse(help.Output).Introduced(await IntroduceAsync(cancellationToken));
        }
        finally
        {
            _learning.Release();
        }
    }

    private async Task<Answer?> AskAsync(string[] arguments, CancellationToken cancellationToken)
    {
        using var process = Launcher.Start(executable, Environment.CurrentDirectory, arguments);

        if (process is null)
        {
            return null;
        }

        process.StandardInput.Close();
        using var patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(Patience);

        try
        {
            var output = process.StandardOutput.ReadToEndAsync(patience.Token);
            var complaints = process.StandardError.ReadToEndAsync(patience.Token);
            await process.WaitForExitAsync(patience.Token);

            return new Answer(process.ExitCode, await output, await complaints);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Launcher.Kill(process);

            return new Answer(-1, string.Empty, $"`claude {string.Join(' ', arguments)}` did not answer in {Patience.TotalSeconds:0} seconds.");
        }
    }

    private async Task<string?> IntroduceAsync(CancellationToken cancellationToken)
    {
        using var process = Launcher.Start(
            executable,
            Environment.CurrentDirectory,
            ["-p", "--input-format", "stream-json", "--output-format", "stream-json", "--verbose"]);

        if (process is null)
        {
            return null;
        }

        using var patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(Patience);

        try
        {
            await process.StandardInput.WriteLineAsync(Messages.Introduction);
            await process.StandardInput.FlushAsync(patience.Token);

            while (await process.StandardOutput.ReadLineAsync(patience.Token) is { } line)
            {
                if (line.Contains("\"control_response\"", StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return null;
        }
        catch (Exception unanswered) when (unanswered is OperationCanceledException or IOException)
        {
            return null;
        }
        finally
        {
            await Launcher.LetGoAsync(process, Patience);
        }
    }

    private static (bool LoggedIn, string? Label)? Account(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("loggedIn", out var flag)
                || flag.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return null;
            }

            string[] parts = [.. new[] { Text(root, "authMethod"), Text(root, "subscriptionType") }.OfType<string>().Where(part => part.Length > 0)];

            return (flag.GetBoolean(), parts.Length > 0 ? string.Join(" · ", parts) : null);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Error Unmeasured(string why) => new("agent.usage_unknown", $"Claude Code couldn't tell its usage: {why}");

    private static string Said(Answer answer, string otherwise) =>
        answer.Complaints.Trim() is { Length: > 0 } complaint ? complaint : otherwise;

    private sealed record Answer(int Code, string Output, string Complaints);
}
