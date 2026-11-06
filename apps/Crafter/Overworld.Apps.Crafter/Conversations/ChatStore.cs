using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Pictures;
using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Crafter.Conversations;

public sealed class ChatStore(string directory, PictureStore pictures)
{
    private static readonly IReadOnlyDictionary<ActKind, string> Does = new Dictionary<ActKind, string>
    {
        [ActKind.Command] = "shell",
        [ActKind.Read] = "read",
        [ActKind.Change] = "edit",
        [ActKind.Create] = "write",
        [ActKind.Search] = "search",
        [ActKind.Browse] = "web",
        [ActKind.Delegate] = "delegate",
        [ActKind.Tool] = "served",
    };

    private static readonly IReadOnlyDictionary<string, ActKind> Tools = new Dictionary<string, ActKind>
    {
        ["Bash"] = ActKind.Command,
        ["Read"] = ActKind.Read,
        ["Edit"] = ActKind.Change,
        ["MultiEdit"] = ActKind.Change,
        ["NotebookEdit"] = ActKind.Change,
        ["Write"] = ActKind.Create,
        ["Grep"] = ActKind.Search,
        ["Glob"] = ActKind.Search,
        ["WebFetch"] = ActKind.Browse,
        ["WebSearch"] = ActKind.Browse,
        ["Task"] = ActKind.Delegate,
        ["Agent"] = ActKind.Delegate,
    };

    private readonly ConcurrentDictionary<string, JsonFile<IReadOnlyList<ChatEntry>>> _files = new();

    public async Task<Result<IReadOnlyList<ChatRecord>>> LoadAsync(string folder, CancellationToken cancellationToken)
    {
        var read = await FileOf(folder).ReadAsync(cancellationToken);

        if (!read.IsSuccess)
        {
            return read.Error;
        }

        try
        {
            return Result.Success<IReadOnlyList<ChatRecord>>([.. read.Value.Select(Record)]);
        }
        catch (ArgumentException unreadable)
        {
            return new Error("file.unreadable", $"The chats of {folder} are not what they should be: {unreadable.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<ChatRecord>>> SaveAsync(string folder, IReadOnlyList<ChatRecord> chats, CancellationToken cancellationToken)
    {
        await FileOf(folder).WriteAsync([.. chats.Select(Entry)], cancellationToken);

        return Result.Success(chats);
    }

    public async Task<Result<IReadOnlyList<ChatRecord>>> KeepAsync(string folder, ChatRecord chat, CancellationToken cancellationToken)
    {
        var entry = Entry(chat);
        var kept = await FileOf(folder).UpdateAsync(
            saved => Result.Success<IReadOnlyList<ChatEntry>>(
                saved.Any(each => each.Id == entry.Id)
                    ? [.. saved.Select(each => each.Id == entry.Id ? entry : each)]
                    : [.. saved, entry]),
            cancellationToken);

        return kept.IsSuccess ? await LoadAsync(folder, cancellationToken) : kept.Error;
    }

    public async Task<Result<IReadOnlyList<ChatRecord>>> RemoveAsync(string folder, ChatId chat, CancellationToken cancellationToken)
    {
        var removed = await FileOf(folder).UpdateAsync(
            saved => Result.Success<IReadOnlyList<ChatEntry>>([.. saved.Where(each => each.Id != chat.Value)]),
            cancellationToken);

        return removed.IsSuccess ? await LoadAsync(folder, cancellationToken) : removed.Error;
    }

    public async Task<Result<IReadOnlyList<ChatRecord>>> ArrangeAsync(string folder, IReadOnlyList<ChatId> newestFirst, CancellationToken cancellationToken)
    {
        var rank = newestFirst.Select((chat, at) => (chat.Value, at)).ToDictionary(each => each.Value, each => each.at);
        var arranged = await FileOf(folder).UpdateAsync(
            saved => Result.Success<IReadOnlyList<ChatEntry>>(
            [
                .. saved.Where(each => rank.ContainsKey(each.Id)).OrderByDescending(each => rank[each.Id]),
                .. saved.Where(each => !rank.ContainsKey(each.Id)),
            ]),
            cancellationToken);

        return arranged.IsSuccess ? await LoadAsync(folder, cancellationToken) : arranged.Error;
    }

    private JsonFile<IReadOnlyList<ChatEntry>> FileOf(string folder) =>
        _files.GetOrAdd(folder, key => new JsonFile<IReadOnlyList<ChatEntry>>(Path.Combine(directory, NameOf(key)), () => []));

    private static string NameOf(string folder)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(folder)))[..12];

        return $"{Path.GetFileName(folder)}-{hash}.json";
    }

    private ChatRecord Record(ChatEntry entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Agent);

        return new(new ChatId(entry.Id), new AgentId(entry.Agent), ChatTitle.Of(entry.Title), entry.SessionId, [.. (entry.Turns ?? []).Select(Turn)]);
    }

    private ChatEntry Entry(ChatRecord record) =>
        new(record.Id.Value, record.Agent.Value, record.Title.Text, record.SessionId, [.. record.Turns.Select(Entry).OfType<TurnEntry>()]);

    private Turn Turn(TurnEntry entry)
    {
        if (entry.Kind == "asked")
        {
            Attachment[] attachments =
            [
                .. (entry.Files ?? []).Select(file => new FileMention(file)),
                .. (entry.Pictures ?? []).Select(pictures.Find).OfType<Picture>(),
            ];

            return new Prompt(entry.Text ?? string.Empty, attachments.Length > 0 ? attachments : [], entry.Skills ?? []);
        }

        IReadOnlyList<Moment> moments = entry.Steps is { Count: > 0 } steps
            ? [.. steps.Select(Moment).OfType<Moment>()]
            : entry.Text.Length > 0 ? [new Speech(entry.Text)] : [];

        return new Reply(moments, Outcome(entry));
    }

    private static Moment? Moment(StepEntry step) => step switch
    {
        { Kind: "wrote", Text: { } text } => new Speech(text),
        { Kind: "thought", Text: { } text } => new Thought(text),
        { Kind: "plan", Plan: { } plan } => new Plan([.. plan.Select(item => new PlanStep(item.Text, Enum.TryParse<StepState>(item.State, ignoreCase: true, out var state) ? state : StepState.Pending))]),
        { Kind: "acting", Id: { } id, Tool: { } tool } => Acting(id, tool, step),
        { Kind: "acted", Id: { } id } => new ActResult(id, step.Failed != true, step.Text ?? string.Empty),
        _ => null,
    };

    private static Act Acting(string id, string tool, StepEntry step)
    {
        var kind = step.Does is { } does
            ? Does.FirstOrDefault(each => each.Value == does).Key
            : Tools.GetValueOrDefault(tool, ActKind.Other);
        var summary = step.Summary is { Length: > 0 } said ? said : tool;

        return new Act(id, kind, kind == ActKind.Tool && step.Summary is { Length: > 0 } ? $"{tool}: {summary}" : summary, Changes(step));
    }

    private static Changes? Changes(StepEntry step) => step switch
    {
        { Added: { } added } => new Changes(added, step.Removed),
        { Stat: { } stat } when stat.Split(' ') is [var plus, .. var rest] && plus.StartsWith('+') && int.TryParse(plus[1..], CultureInfo.InvariantCulture, out var added) =>
            new Changes(added, rest is [var minus] && minus.StartsWith('−') && int.TryParse(minus[1..], CultureInfo.InvariantCulture, out var removed) ? removed : null),
        _ => null,
    };

    private static Outcome Outcome(TurnEntry entry) => entry.Ending switch
    {
        "finished" => new Finished(),
        "stopped" => new Stopped(),
        "cutShort" => new CutShort(),
        "refused" => new Refused(entry.Detail),
        _ => new Failed(entry.Detail ?? "The answer could not be read back."),
    };

    private TurnEntry? Entry(Turn turn) => turn switch
    {
        Prompt prompt => new TurnEntry(
            "asked",
            prompt.Text,
            Pictures: Listed([.. prompt.Attachments.OfType<Picture>().Select(pictures.Keep).OfType<string>()]),
            Files: Listed([.. prompt.Attachments.OfType<FileMention>().Select(file => file.Path)]),
            Skills: Listed(prompt.Skills)),
        Reply reply => new TurnEntry(
            "answered",
            string.Concat(reply.Moments.OfType<Speech>().Select(speech => speech.Text)),
            Name(reply.Outcome),
            Detail(reply.Outcome),
            reply.Moments.Any(moment => moment is not Speech) ? [.. reply.Moments.Select(Step).OfType<StepEntry>()] : null),
        _ => null,
    };

    private static IReadOnlyList<string>? Listed(IReadOnlyList<string> items) => items.Count > 0 ? items : null;

    private static StepEntry? Step(Moment moment) => moment switch
    {
        Speech speech => new StepEntry("wrote", Text: speech.Text),
        Thought thought => new StepEntry("thought", Text: thought.Text),
        Plan plan => new StepEntry("plan", Plan: [.. plan.Steps.Select(step => new PlanItemEntry(step.Text, step.State.ToString().ToLowerInvariant()))]),
        Act act => Acting(act),
        ActResult result => new StepEntry("acted", Id: result.Id, Text: result.Output, Failed: result.Succeeded ? null : true),
        _ => null,
    };

    private static StepEntry Acting(Act act)
    {
        var (tool, summary) = act.Kind == ActKind.Tool && act.Subject.Split(": ", 2) is [var server, var rest]
            ? (server, rest)
            : (act.Kind.ToString(), act.Subject);

        return new StepEntry("acting", Id: act.Id, Tool: tool, Does: Does.GetValueOrDefault(act.Kind), Summary: summary, Added: act.Changes?.Added, Removed: act.Changes?.Removed);
    }

    private static string Name(Outcome outcome) => outcome switch
    {
        Finished => "finished",
        Stopped => "stopped",
        CutShort => "cutShort",
        Refused => "refused",
        _ => "failed",
    };

    private static string? Detail(Outcome outcome) => outcome switch
    {
        Refused refused => refused.Reason,
        Failed failed => failed.Reason,
        _ => null,
    };
}
