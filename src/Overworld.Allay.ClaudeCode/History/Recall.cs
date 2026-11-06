using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using static Overworld.Allay.ClaudeCode.Protocol.JsonFields;

namespace Overworld.Allay.ClaudeCode.History;

internal sealed partial class Recall(string home)
{
    private const int Tail = 512 * 1024;

    private const int Longest = 40;

    private static readonly string[] Spoken = ["\"type\":\"user\"", "\"type\":\"assistant\""];

    public static string Home =>
        Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");

    public Task<Recollection> ListAsync(string folder, int latest, CancellationToken cancellationToken) =>
        Task.Run(() => Remember(folder, latest), cancellationToken);

    public Task<IReadOnlyList<Turn>> ReplayAsync(string folder, string session, CancellationToken cancellationToken) =>
        Task.Run<IReadOnlyList<Turn>>(() => Retell(Path.Combine(FolderOf(folder), session + ".jsonl"), session), cancellationToken);

    private static string Title(string prompt)
    {
        var line = prompt.Trim().Split('\n')[0].Trim();

        return line.Length <= Longest ? line : line[..(Longest - 1)].TrimEnd() + "…";
    }

    private static PastSession? Summarize(FileInfo session)
    {
        if (First(session) is not { } first)
        {
            return null;
        }

        var last = Last(session);

        return new PastSession(
            Path.GetFileNameWithoutExtension(session.Name),
            Title(first),
            session.LastWriteTimeUtc,
            last is not null && last != first ? new Prompt(last, [], []) : null);
    }

    private static string? First(FileInfo session) => Entries(Lines(session.FullName)).Select(Prompt).FirstOrDefault(prompt => prompt is not null);

    private static string? Last(FileInfo session) => Entries(TailLines(session.FullName).Reverse()).Select(Prompt).FirstOrDefault(prompt => prompt is not null);

    private static List<Turn> Retell(string file, string sessionId)
    {
        List<Turn> turns = [];
        if (Path.GetFileName(sessionId) != sessionId)
        {
            return turns;
        }

        var answer = new StringBuilder();

        foreach (var entry in Entries(Lines(file)))
        {
            if (Prompt(entry, commands: true) is { } prompt)
            {
                Close(turns, answer);
                turns.Add(new Prompt(prompt, [], []));
            }
            else if (turns.Count > 0 && Answer(entry) is { } text)
            {
                answer.Append(answer.Length > 0 ? "\n\n" : string.Empty).Append(text);
            }
        }

        Close(turns, answer);

        return turns;
    }

    private static void Close(List<Turn> turns, StringBuilder answer)
    {
        if (answer.Length > 0)
        {
            turns.Add(new Reply([new Speech(answer.ToString())], new Finished()));
            answer.Clear();
        }
    }

    private static string? Prompt(JsonElement entry) => Prompt(entry, commands: false);

    private static string? Prompt(JsonElement entry, bool commands)
    {
        if (Text(entry, "type") != "user"
            || Flag(entry, "isMeta")
            || Flag(entry, "isSidechain")
            || Flag(entry, "isCompactSummary")
            || Flag(entry, "isVisibleInTranscriptOnly"))
        {
            return null;
        }

        var said = Message(entry) switch
        {
            { ValueKind: JsonValueKind.String } content => content.GetString(),
            { ValueKind: JsonValueKind.Array } blocks when !Blocks(blocks).Any(block => Text(block, "type") == "tool_result") => Written(blocks),
            _ => null,
        };

        return said is null ? null : Typed(said, commands);
    }

    private static string? Typed(string said, bool commands)
    {
        var text = CalledSkills().Replace(Reminders().Replace(said, string.Empty), string.Empty).Trim();
        if (text.Length == 0 || text.StartsWith("Caveat:", StringComparison.Ordinal) || text.StartsWith("[Request interrupted by user", StringComparison.Ordinal))
        {
            return null;
        }

        if (Tagged("command-name", text) is { } command)
        {
            var arguments = Tagged("command-args", text);

            return commands ? string.IsNullOrEmpty(arguments) ? command : $"{command} {arguments}" : null;
        }

        if (text.StartsWith("<bash-input>", StringComparison.Ordinal))
        {
            var input = Tagged("bash-input", text);

            return commands && !string.IsNullOrEmpty(input) ? $"! {input}" : null;
        }

        return Injected().IsMatch(text) ? null : text;
    }

    private static string? Tagged(string tag, string text) =>
        Regex.Match(text, $"<{tag}>(.*?)</{tag}>", RegexOptions.Singleline) is { Success: true } found ? found.Groups[1].Value.Trim() : null;

    private static string? Answer(JsonElement entry) =>
        Text(entry, "type") == "assistant" && !Flag(entry, "isSidechain") && Message(entry) is { ValueKind: JsonValueKind.Array } blocks
            ? Written(blocks)?.Trim() is { Length: > 0 } text ? text : null
            : null;

    private static string? Written(JsonElement blocks)
    {
        var texts = Blocks(blocks).Where(block => Text(block, "type") == "text").Select(block => Text(block, "text")).OfType<string>().ToList();

        return texts.Count > 0 ? string.Join("\n\n", texts) : null;
    }

    private static IEnumerable<JsonElement> Blocks(JsonElement blocks) => blocks.EnumerateArray().Where(block => block.ValueKind == JsonValueKind.Object);

    private static JsonElement? Message(JsonElement entry) =>
        entry.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object && message.TryGetProperty("content", out var content)
            ? content
            : null;

    private static IEnumerable<JsonElement> Entries(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (Spoken.Any(spoken => line.Contains(spoken, StringComparison.Ordinal)) && Parse(line) is { } entry)
            {
                yield return entry;
            }
        }
    }

    private static IEnumerable<string> Lines(string file)
    {
        IEnumerable<string> lines;
        try
        {
            lines = File.ReadLines(file);
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var line in lines)
        {
            yield return line;
        }
    }

    private static string[] TailLines(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            var start = Math.Max(0, stream.Length - Tail);
            stream.Seek(start, SeekOrigin.Begin);

            using var reader = new StreamReader(stream);
            var lines = reader.ReadToEnd().Split('\n');

            return start > 0 ? lines[1..] : lines;
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static JsonElement? Parse(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);

            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private Recollection Remember(string folder, int latest)
    {
        List<PastSession> chats = [];
        var total = 0;

        foreach (var session in Sessions(folder))
        {
            if (chats.Count < latest && Summarize(session) is { } chat)
            {
                chats.Add(chat);
                total++;
            }
            else if (chats.Count >= latest && First(session) is not null)
            {
                total++;
            }
        }

        return new Recollection(chats, total);
    }

    private IEnumerable<FileInfo> Sessions(string folder)
    {
        var found = new DirectoryInfo(FolderOf(folder));

        return found.Exists ? found.EnumerateFiles("*.jsonl").OrderByDescending(file => file.LastWriteTimeUtc) : [];
    }

    private string FolderOf(string folder) => Path.Combine(home, "projects", Unsafe().Replace(folder, "-"));

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex Unsafe();

    [GeneratedRegex("<system-reminder>.*?</system-reminder>", RegexOptions.Singleline)]
    private static partial Regex Reminders();

    [GeneratedRegex("<(crafter-skill|skill|brief-update)\\b[^>]*>.*?</(crafter-skill|skill|brief-update)>\\s*", RegexOptions.Singleline)]
    private static partial Regex CalledSkills();

    [GeneratedRegex("^<(local-command-stdout|local-command-stderr|local-command-caveat|bash-stdout|bash-stderr|task-notification)>")]
    private static partial Regex Injected();
}
