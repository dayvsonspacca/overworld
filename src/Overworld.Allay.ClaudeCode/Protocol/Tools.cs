using System.Text.Json;
using Overworld.Allay.Conversations;
using static Overworld.Allay.ClaudeCode.Protocol.JsonFields;

namespace Overworld.Allay.ClaudeCode.Protocol;

internal static class Tools
{
    public const string Plans = "TodoWrite";

    public const string AsksTheUser = "AskUserQuestion";

    public static ActKind Kind(string tool) => tool switch
    {
        "Bash" => ActKind.Command,
        "Read" => ActKind.Read,
        "Edit" or "MultiEdit" or "NotebookEdit" => ActKind.Change,
        "Write" => ActKind.Create,
        "Grep" or "Glob" => ActKind.Search,
        "WebFetch" or "WebSearch" => ActKind.Browse,
        "Task" or "Agent" => ActKind.Delegate,
        _ => Served(tool) is not null ? ActKind.Tool : ActKind.Other,
    };

    public static (ActKind Kind, string Subject, Changes? Changes) Shown(string tool, JsonElement input, string? folder)
    {
        string? Field(string name) => Text(input, name);

        string File() => Relative(Field("file_path") ?? Field("notebook_path") ?? Field("path") ?? string.Empty, folder);

        if (Served(tool) is { } served)
        {
            var action = served.Tool.Replace('_', ' ').Replace('-', ' ').Trim();
            var argument = FirstArgument(input) is { } first ? Relative(FirstLine(first), folder) : null;

            return (ActKind.Tool, argument is { Length: > 0 } ? $"{served.Server}: {action} · {argument}" : $"{served.Server}: {action}", null);
        }

        return tool switch
        {
            "Bash" => (ActKind.Command, FirstLine(Field("command") ?? string.Empty), null),
            "Read" => (ActKind.Read, File(), null),
            "Edit" => (ActKind.Change, File(), new Changes(Lines(Field("new_string")), Lines(Field("old_string")))),
            "MultiEdit" => (ActKind.Change, File(), new Changes(Edits(input, "new_string"), Edits(input, "old_string"))),
            "NotebookEdit" => (ActKind.Change, File(), null),
            "Write" => (ActKind.Create, File(), new Changes(Lines(Field("content")))),
            "Grep" or "Glob" => (ActKind.Search, Field("pattern") ?? string.Empty, null),
            "WebFetch" => (ActKind.Browse, Field("url") ?? string.Empty, null),
            "WebSearch" => (ActKind.Browse, Field("query") ?? string.Empty, null),
            "Task" or "Agent" => (ActKind.Delegate, Field("description") ?? string.Empty, null),
            _ => (ActKind.Other, $"{tool}: {Field("description") ?? Field("command") ?? Field("file_path") ?? string.Empty}".TrimEnd(' ', ':'), null),
        };
    }

    public static IReadOnlyList<PlanStep> Steps(JsonElement input) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty("todos", out var todos) && todos.ValueKind == JsonValueKind.Array
            ? [.. todos.EnumerateArray()
                .Where(todo => Text(todo, "content") is { Length: > 0 })
                .Select(todo => new PlanStep(Text(todo, "content")!, Text(todo, "status") switch
                {
                    "in_progress" => StepState.Doing,
                    "completed" => StepState.Done,
                    _ => StepState.Pending,
                }))]
            : [];

    private static (string Server, string Tool)? Served(string tool)
    {
        if (!tool.StartsWith("mcp__", StringComparison.Ordinal))
        {
            return null;
        }

        var parts = tool["mcp__".Length..].Split("__", 2);

        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return null;
        }

        var server = parts[0].StartsWith("claude_ai_", StringComparison.Ordinal) ? parts[0]["claude_ai_".Length..] : parts[0];

        return (server.Replace('_', ' '), parts[1]);
    }

    private static string? FirstArgument(JsonElement input) =>
        input.ValueKind == JsonValueKind.Object
            ? input.EnumerateObject()
                .Where(field => field.Value.ValueKind == JsonValueKind.String)
                .Select(field => field.Value.GetString()!.Trim())
                .FirstOrDefault(text => text.Length > 0)
            : null;

    private static string Relative(string path, string? folder) =>
        folder is not null && path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? path[(folder.Length + 1)..] : path;

    private static string FirstLine(string text) => text.Split('\n', 2)[0].Trim();

    private static int Lines(string? text) => string.IsNullOrEmpty(text) ? 0 : text.TrimEnd('\n').Split('\n').Length;

    private static int Edits(JsonElement input, string field) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty("edits", out var edits) && edits.ValueKind == JsonValueKind.Array
            ? edits.EnumerateArray().Sum(edit => edit.ValueKind == JsonValueKind.Object && edit.TryGetProperty(field, out var text) && text.ValueKind == JsonValueKind.String ? Lines(text.GetString()) : 0)
            : 0;
}
