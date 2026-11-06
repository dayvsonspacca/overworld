using System.Runtime.CompilerServices;
using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.ClaudeCode.Cli;
using Overworld.Allay.ClaudeCode.Protocol;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.ClaudeCode;

public sealed class Session : ISession
{
    private readonly string _executable;

    private readonly Func<Capabilities> _capabilities;

    private readonly string _shelves;

    private readonly Brief _known;

    private readonly List<string> _allowed = [];

    private readonly List<string> _directories = [];

    private Brief _told;

    private Brief _latest;

    private SkillShelf? _shelf;

    private Run? _run;

    internal Session(string executable, Func<Capabilities> capabilities, string shelves, Brief brief, string? resume)
    {
        _executable = executable;
        _capabilities = capabilities;
        _shelves = shelves;
        _known = _told = _latest = brief;
        Id = resume;
    }

    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke;

    public string? Id { get; private set; }

    public IReadOnlyList<Job> Jobs => _run?.Jobs ?? [];

    public async IAsyncEnumerable<Moment> ReplyAsync(Prompt prompt, Setup setup, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = Options.Of(_executable, setup, _capabilities());

        if (await RunningAsync(options) is not { } live)
        {
            yield return new Failed($"Could not start '{_executable}'. Is Claude Code installed and on the PATH?");

            yield break;
        }

        var text = BriefText.Calling(BriefText.Pointing(prompt.Text, prompt.Attachments), prompt.Skills, _latest.Skills, _shelf);

        if (BriefText.Update(_latest.Since(_told), _shelf) is { } update)
        {
            text = $"{update}\n\n{text}";
        }

        _told = _latest;

        var turn = live.Ask(text, [.. prompt.Attachments.OfType<Picture>()]);

        await foreach (var moment in HeardAsync(turn, cancellationToken))
        {
            yield return moment;
        }

        if (cancellationToken.IsCancellationRequested && !turn.Done.IsCompleted)
        {
            await live.HaltAsync(turn);

            yield return new Stopped();
        }
    }

    public void Rebrief(Brief brief) => _latest = brief;

    public void Decide(string request, Decision decision)
    {
        if (_run is not { } live)
        {
            return;
        }

        var suggestions = live.Respond(request, decision);

        if (decision is AllowAlways)
        {
            var (rules, folders) = Permissions.Granted(suggestions);
            _allowed.AddRange(rules.Except(_allowed));
            _directories.AddRange(folders.Except(_directories));
        }
    }

    public string? Output(string job) => _run?.OutputOf(job) is { } file ? JobOutput.Latest(file) : null;

    public void Stop(string job) => _run?.Stop(job);

    public void End()
    {
        if (_run is { } running)
        {
            _ = Task.Run(running.Kill);
        }
    }

    private static async IAsyncEnumerable<Moment> HeardAsync(Run.Turn turn, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                if (!await turn.Moments.WaitToReadAsync(cancellationToken))
                {
                    yield break;
                }
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            while (turn.Moments.TryRead(out var moment))
            {
                yield return moment;
            }
        }
    }

    private async Task<Run?> RunningAsync(Options wanted)
    {
        if (_run is { IsOpen: true } live && (live.Options == wanted || live.Jobs.Count > 0))
        {
            return live;
        }

        if (_run is { } previous)
        {
            if (previous.IsOpen)
            {
                previous.Kill();
            }

            await previous.FinishAsync();
        }

        _shelf = Shelved(_latest.Skills);

        var started = Run.Start(wanted, _latest.Folder, [.. wanted.Arguments(Id), .. Granted()]);

        if (started is null)
        {
            return null;
        }

        started.Known += session => Id = session;
        started.JobsChanged += jobs => JobsChanged?.Invoke(this, jobs);
        started.Woke += turn => Woke?.Invoke(this, HeardAsync(turn));
        started.Begin();

        return _run = started;
    }

    private SkillShelf? Shelved(IReadOnlyList<Skill> skills)
    {
        try
        {
            return SkillShelf.Write(_shelves, skills);
        }
        catch (Exception unwritable) when (unwritable is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private IEnumerable<string> Granted()
    {
        yield return "--disable-slash-commands";

        if (BriefText.System(_known, _shelf) is { } system)
        {
            yield return "--append-system-prompt";
            yield return system;
        }

        var servers = _latest.Tools;

        if (servers.Count > 0)
        {
            yield return "--mcp-config";
            yield return McpConfig.Of(servers);
        }

        string[] rules = [.. _allowed, .. servers.SelectMany(server => server.FreeTools.Select(tool => $"mcp__{server.Name}__{tool}"))];

        if (rules.Length > 0)
        {
            yield return "--allowedTools";

            foreach (var rule in rules)
            {
                yield return rule;
            }
        }

        string[] reachable = [.. _directories, .. _shelf is { } shelf ? [shelf.Folder] : Array.Empty<string>()];

        if (reachable.Length > 0)
        {
            yield return "--add-dir";

            foreach (var directory in reachable)
            {
                yield return directory;
            }
        }
    }
}
