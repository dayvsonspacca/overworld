using System.Text.Json;
using Overworld.Allay.Conversations;
using static Overworld.Allay.ClaudeCode.Protocol.JsonFields;

namespace Overworld.Allay.ClaudeCode.Protocol;

internal static class Permissions
{
    private const string Allows = "allow";

    public static IReadOnlyList<Allowance> Remembers(JsonElement suggestions)
    {
        List<Allowance> allowances = [];

        foreach (var suggestion in Each(suggestions))
        {
            var reach = ReachOf(Text(suggestion, "destination"));
            allowances.AddRange((Text(suggestion, "type") switch
            {
                "addRules" when Text(suggestion, "behavior") == Allows => Rules(suggestion),
                "addDirectories" => Strings(suggestion, "directories"),
                "setMode" when Text(suggestion, "mode") is { } mode => [$"{mode} mode"],
                _ => [],
            }).Select(what => new Allowance(what, reach)));
        }

        return [.. allowances.Distinct()];
    }

    public static (IReadOnlyList<string> Rules, IReadOnlyList<string> Directories) Granted(string? suggestions)
    {
        if (suggestions is null)
        {
            return ([], []);
        }

        try
        {
            using var document = JsonDocument.Parse(suggestions);
            var each = Each(document.RootElement).ToList();

            return (
                [.. each.Where(suggestion => Text(suggestion, "type") == "addRules" && Text(suggestion, "behavior") == Allows).SelectMany(Rules)],
                [.. each.Where(suggestion => Text(suggestion, "type") == "addDirectories").SelectMany(suggestion => Strings(suggestion, "directories"))]);
        }
        catch (JsonException)
        {
            return ([], []);
        }
    }

    private static Reach ReachOf(string? destination) => destination switch
    {
        "userSettings" => Reach.Everywhere,
        "projectSettings" or "localSettings" => Reach.Folder,
        _ => Reach.Conversation,
    };

    private static IEnumerable<string> Rules(JsonElement suggestion) =>
        suggestion.TryGetProperty("rules", out var rules) && rules.ValueKind == JsonValueKind.Array
            ? Each(rules)
                .Where(rule => Text(rule, "toolName") is not null)
                .Select(rule => Text(rule, "ruleContent") is { Length: > 0 } content ? $"{Text(rule, "toolName")}({content})" : Text(rule, "toolName")!)
            : [];

    private static IEnumerable<string> Strings(JsonElement holder, string property) =>
        holder.TryGetProperty(property, out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!)
            : [];

    private static IEnumerable<JsonElement> Each(JsonElement array) =>
        array.ValueKind == JsonValueKind.Array ? array.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object) : [];
}
