using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Overworld.Apps.Crafter.Agents;
using Overworld.Apps.Crafter.Conversations.Replies;
using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Crafter.Conversations;

public sealed class ChatStore(string directory)
{
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

    private JsonFile<IReadOnlyList<ChatEntry>> FileOf(string folder) =>
        _files.GetOrAdd(folder, key => new JsonFile<IReadOnlyList<ChatEntry>>(Path.Combine(directory, NameOf(key)), () => []));

    private static string NameOf(string folder)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(folder)))[..12];

        return $"{Path.GetFileName(folder)}-{hash}.json";
    }

    private static ChatRecord Record(ChatEntry entry) =>
        new(new ChatId(entry.Id), new AgentId(entry.Agent), ChatTitle.Of(entry.Title), entry.SessionId, [.. (entry.Turns ?? []).Select(Turn)]);

    private static ChatEntry Entry(ChatRecord record) =>
        new(record.Id.Value, record.Agent.Value, record.Title.Text, record.SessionId, [.. record.Turns.Select(Entry)]);

    private static Turn Turn(TurnEntry entry) => entry.Kind switch
    {
        "asked" => new Asked(new Prompt(entry.Text)),
        _ => new Answered(entry.Text, Ending(entry)) { Steps = [.. (entry.Steps ?? []).Select(Step).OfType<Reply>()] },
    };

    private static Reply? Step(StepEntry step) => step switch
    {
        { Kind: "wrote", Text: { } text } => new Wrote(text),
        { Kind: "acting", Id: { } id, Tool: { } tool } => new Acting(id, new Tool(Enum.TryParse<ToolKind>(step.Does, ignoreCase: true, out var kind) ? kind : ToolKind.Other, tool), step.Summary ?? string.Empty, Changes(step)),
        { Kind: "acted", Id: { } id } => new Acted(id, step.Failed ?? false, step.Text ?? string.Empty),
        _ => null,
    };

    private static StepEntry? Entry(Reply step) => step switch
    {
        Wrote wrote => new StepEntry("wrote", Text: wrote.Fragment),
        Acting acting => new StepEntry("acting", Id: acting.Id, Tool: acting.Tool.Name, Does: acting.Tool.Kind == ToolKind.Other ? null : JsonNamingPolicy.CamelCase.ConvertName(acting.Tool.Kind.ToString()), Summary: acting.Summary, Added: acting.Changes?.Added, Removed: acting.Changes?.Removed),
        Acted acted => new StepEntry("acted", Id: acted.Id, Text: acted.Output, Failed: acted.Failed ? true : null),
        _ => null,
    };

    private static Changes? Changes(StepEntry step) => step switch
    {
        { Added: { } added } => new Changes(added, step.Removed),
        { Stat: { } stat } when stat.Split(' ') is [var plus, .. var rest] && plus.StartsWith('+') && int.TryParse(plus[1..], CultureInfo.InvariantCulture, out var added) =>
            new Changes(added, rest is [var minus] && minus.StartsWith('−') && int.TryParse(minus[1..], CultureInfo.InvariantCulture, out var removed) ? removed : null),
        _ => null,
    };

    private static Ending Ending(TurnEntry entry) => entry.Ending switch
    {
        "finished" => new Finished(),
        "stopped" => new Stopped(),
        "cutShort" => new CutShort(),
        "refused" => new Refused(entry.Detail),
        _ => new Failed(entry.Detail ?? "The answer could not be read back."),
    };

    private static TurnEntry Entry(Turn turn) => turn switch
    {
        Asked asked => new TurnEntry("asked", asked.Prompt.Text),
        Answered answered => new TurnEntry(
            "answered",
            answered.Text,
            Name(answered.Ending),
            Detail(answered.Ending),
            answered.Steps.Count > 0 ? [.. answered.Steps.Select(Entry).OfType<StepEntry>()] : null),
        _ => throw new ArgumentOutOfRangeException(nameof(turn), turn, "Only asked and answered turns are kept."),
    };

    private static string Name(Ending ending) => ending switch
    {
        Finished => "finished",
        Stopped => "stopped",
        CutShort => "cutShort",
        Refused => "refused",
        _ => "failed",
    };

    private static string? Detail(Ending ending) => ending switch
    {
        Refused refused => refused.Reason,
        Failed failed => failed.Reason,
        _ => null,
    };
}
