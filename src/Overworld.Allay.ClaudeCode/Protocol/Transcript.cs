using System.Text.Json;
using System.Text.RegularExpressions;
using Overworld.Allay.ClaudeCode.Cli;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using static Overworld.Allay.ClaudeCode.Protocol.JsonFields;

namespace Overworld.Allay.ClaudeCode.Protocol;

internal sealed partial class Transcript(string? folder = null)
{
    private const string Watches = "Monitor";

    private static readonly string[] Over = ["completed", "failed", "killed", "stopped"];

    private readonly Dictionary<string, Asked> _requests = [];

    private readonly Dictionary<string, string> _tools = [];

    private readonly HashSet<string> _hidden = [];

    private readonly Dictionary<string, string> _outputs = [];

    private readonly Dictionary<string, string> _owners = [];

    private readonly List<Job> _jobs = [];

    public string? SessionId { get; private set; }

    public IReadOnlyList<Job> Jobs => _jobs;

    public bool Opened { get; private set; }

    public string? Started { get; private set; }

    public bool JobsMoved { get; private set; }

    public string InputOf(string request) => _requests.GetValueOrDefault(request)?.Input ?? "{}";

    public string? SuggestionsOf(string request) => _requests.GetValueOrDefault(request)?.Suggestions;

    public string? OutputOf(string job) => _owners.GetValueOrDefault(job) is { } owner ? _outputs.GetValueOrDefault(owner) : null;

    public IReadOnlyList<Moment> Hear(string line)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return [];
        }

        Opened = false;
        Started = null;
        JobsMoved = false;

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            SessionId = Text(root, "session_id") ?? SessionId;

            return Text(root, "type") switch
            {
                "system" when Text(root, "subtype") == "init" => Introduction(root),
                "system" when Text(root, "subtype") == "compact_boundary" => [new Notice(NoticeKind.Context, "The conversation was summarized to fit the context.")],
                "system" => Background(root),
                "command_lifecycle" => Lifecycle(root),
                "stream_event" => Fragment(root),
                "assistant" => Acts(root),
                "user" => Results(root),
                "control_request" => Request(root),
                "control_cancel_request" when Text(root, "request_id") is { } request => [new Withdrawn(request)],
                "result" => [Conclusion(root)],
                _ => [],
            };
        }
    }

    private IReadOnlyList<Moment> Introduction(JsonElement root)
    {
        Opened = true;

        return Text(root, "model") is { } model ? [new Notice(NoticeKind.Model, ModelName.Of(model))] : [];
    }

    private IReadOnlyList<Moment> Lifecycle(JsonElement root)
    {
        if (Text(root, "state") == "started")
        {
            Started = Text(root, "command_uuid");
        }

        return [];
    }

    private IReadOnlyList<Moment> Background(JsonElement root)
    {
        switch (Text(root, "subtype"))
        {
            case "task_started" when Text(root, "task_id") is { } id:
                Track(id, Text(root, "task_type"), Text(root, "tool_use_id"), Text(root, "description"));
                break;
            case "task_updated" when Text(root, "task_id") is { } id && root.TryGetProperty("patch", out var patch) && Over.Contains(Text(patch, "status")):
                Forget(id);
                break;
            case "task_notification" when Text(root, "task_id") is { } id && Over.Contains(Text(root, "status")):
                Forget(id);
                break;
            case "background_tasks_changed" when root.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array:
                var listed = tasks.EnumerateArray().Where(task => Text(task, "task_id") is not null).ToList();

                foreach (var gone in _jobs.Where(job => listed.All(task => Text(task, "task_id") != job.Id)).ToList())
                {
                    Forget(gone.Id);
                }

                foreach (var task in listed)
                {
                    Track(Text(task, "task_id")!, Text(task, "task_type"), Text(task, "tool_use_id"), Text(task, "description"));
                }

                break;
        }

        return [];
    }

    private void Track(string id, string? type, string? tool, string? description)
    {
        var at = _jobs.FindIndex(job => job.Id == id);

        if (tool is not null)
        {
            _owners[id] = tool;
        }

        var owner = _owners.GetValueOrDefault(id);
        var kind = type switch
        {
            "local_bash" when owner is not null && _tools.GetValueOrDefault(owner) == Watches => JobKind.Monitor,
            "local_bash" => JobKind.Command,
            null when at >= 0 => _jobs[at].Kind,
            _ => JobKind.Agent,
        };
        var job = new Job(id, kind, description is { Length: > 0 } said ? said : at >= 0 ? _jobs[at].Description : id, OutputOf(id) is not null);

        if (at < 0)
        {
            _jobs.Add(job);
            JobsMoved = true;
        }
        else if (_jobs[at] != job)
        {
            _jobs[at] = job;
            JobsMoved = true;
        }
    }

    private void Forget(string id) => JobsMoved |= _jobs.RemoveAll(job => job.Id == id) > 0;

    private static IReadOnlyList<Moment> Fragment(JsonElement root)
    {
        if (!root.TryGetProperty("event", out var streamed)
            || Text(streamed, "type") != "content_block_delta"
            || !streamed.TryGetProperty("delta", out var delta))
        {
            return [];
        }

        return Text(delta, "type") switch
        {
            "text_delta" when Text(delta, "text") is { Length: > 0 } text => [new Speech(text)],
            "thinking_delta" when Text(delta, "thinking") is { Length: > 0 } thought => [new Thought(thought)],
            _ => [],
        };
    }

    private IReadOnlyList<Moment> Acts(JsonElement root) =>
        [
            .. Blocks(root)
                .Where(block => Text(block, "type") == "tool_use" && Text(block, "id") is not null && Text(block, "name") is not null)
                .Select(block => Used(Text(block, "id")!, Text(block, "name")!, Input(block)))
                .OfType<Moment>(),
        ];

    private Moment? Used(string id, string tool, JsonElement input)
    {
        _tools[id] = tool;

        switch (tool)
        {
            case Tools.Plans:
                _hidden.Add(id);

                return new Plan(Tools.Steps(input));
            case Tools.AsksTheUser:
                _hidden.Add(id);

                return null;
            default:
                var (kind, subject, changes) = Tools.Shown(tool, input, folder);

                return new Act(id, kind, subject, changes);
        }
    }

    private IReadOnlyList<Moment> Results(JsonElement root) =>
        [
            .. Blocks(root)
                .Where(block => Text(block, "type") == "tool_result" && Text(block, "tool_use_id") is { } tool && !_hidden.Contains(tool))
                .Select(block => Result(Text(block, "tool_use_id")!, block)),
        ];

    private ActResult Result(string tool, JsonElement block)
    {
        var said = Said(block);

        if (Written().Match(said) is { Success: true } written)
        {
            _outputs[tool] = written.Groups[1].Value;

            foreach (var (job, owner) in _owners.Where(each => each.Value == tool && _jobs.Any(listed => listed.Id == each.Key)).ToList())
            {
                Track(job, null, owner, null);
            }
        }

        return new ActResult(tool, !Flag(block, "is_error"), said);
    }

    private IReadOnlyList<Moment> Request(JsonElement root)
    {
        if (Text(root, "request_id") is not { } request
            || !root.TryGetProperty("request", out var asked)
            || Text(asked, "subtype") != "can_use_tool"
            || Text(asked, "tool_name") is not { } tool)
        {
            return [];
        }

        var input = Input(asked);
        var suggestions = asked.TryGetProperty("permission_suggestions", out var offered) && offered.ValueKind == JsonValueKind.Array && offered.GetArrayLength() > 0
            ? offered
            : default;
        _requests[request] = new Asked(
            input.ValueKind == JsonValueKind.Object ? input.GetRawText() : "{}",
            suggestions.ValueKind == JsonValueKind.Array ? suggestions.GetRawText() : null);

        if (tool == Tools.AsksTheUser && QuestionsIn(input) is { Count: > 0 } questions)
        {
            return [new Questions(request, questions)];
        }

        var (kind, subject, _) = Tools.Shown(tool, input, folder);

        return [new Permission(request, kind, subject, Text(asked, "description"), Permissions.Remembers(suggestions))];
    }

    private static IReadOnlyList<Question> QuestionsIn(JsonElement input) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array
            ? [.. questions.EnumerateArray().Select(Posed).OfType<Question>()]
            : [];

    private static Question? Posed(JsonElement question)
    {
        if (question.ValueKind != JsonValueKind.Object
            || Text(question, "question") is not { } text
            || !question.TryGetProperty("options", out var options)
            || options.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        IReadOnlyList<Option> offered =
        [
            .. options.EnumerateArray()
                .Where(option => option.ValueKind == JsonValueKind.Object && Text(option, "label") is { Length: > 0 })
                .Select(option => new Option(Text(option, "label")!, Text(option, "description") ?? string.Empty)),
        ];

        return new Question(text, Text(question, "header") ?? string.Empty, Flag(question, "multiSelect"), offered);
    }

    private static JsonElement Input(JsonElement holder) => holder.TryGetProperty("input", out var input) ? input : default;

    private static IEnumerable<JsonElement> Blocks(JsonElement root) =>
        root.TryGetProperty("message", out var message)
        && message.ValueKind == JsonValueKind.Object
        && message.TryGetProperty("content", out var content)
        && content.ValueKind == JsonValueKind.Array
            ? content.EnumerateArray().Where(block => block.ValueKind == JsonValueKind.Object)
            : [];

    private static string Said(JsonElement result) =>
        result.TryGetProperty("content", out var content)
            ? content.ValueKind switch
            {
                JsonValueKind.String => content.GetString()!,
                JsonValueKind.Array => string.Join('\n', content.EnumerateArray().Select(part => part.ValueKind == JsonValueKind.Object ? Text(part, "text") : null).OfType<string>()),
                _ => string.Empty,
            }
            : string.Empty;

    private static Outcome Conclusion(JsonElement root)
    {
        if (Flag(root, "is_error"))
        {
            return new Failed(Text(root, "result") ?? Text(root, "subtype") ?? "Claude Code failed without saying why.");
        }

        return Text(root, "stop_reason") switch
        {
            "max_tokens" => new CutShort(),
            "refusal" => new Refused(root.TryGetProperty("stop_details", out var details) ? Text(details, "category") : null),
            _ => new Finished(),
        };
    }

    [GeneratedRegex(@"written to: (\S+\.output)")]
    private static partial Regex Written();

    private sealed record Asked(string Input, string? Suggestions);
}
