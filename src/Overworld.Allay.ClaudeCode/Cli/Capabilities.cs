using System.Text.Json;
using System.Text.RegularExpressions;
using Overworld.Allay.Settings;

namespace Overworld.Allay.ClaudeCode.Cli;

internal sealed partial record Capabilities(IReadOnlyList<ModelOption> Models, IReadOnlyList<string> Efforts)
{
    private const string Recommended = "default";

    public static readonly Capabilities Unknown = new([], []);

    public string? Highest => Efforts.Count > 0 ? Efforts[^1] : null;

    public static Capabilities Parse(string help)
    {
        var flat = Spaces().Replace(help, " ");

        return new Capabilities(
            [.. Listed(Model().Match(flat), Quoted()).Select(alias => new ModelOption(alias, ModelName.Of(alias)))],
            Listed(Effort().Match(flat), Word()));
    }

    public Capabilities Introduced(string? answer) => Offered(answer) is { Count: > 0 } models ? this with { Models = models } : this;

    public bool Equals(Capabilities? other) =>
        other is not null && Models.SequenceEqual(other.Models) && Efforts.SequenceEqual(other.Efforts);

    public override int GetHashCode() => HashCode.Combine(Models.Count, Efforts.Count);

    private static IReadOnlyList<ModelOption> Offered(string? answer)
    {
        if (answer is null)
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(answer);

            return document.RootElement.TryGetProperty("response", out var outer)
                && outer.TryGetProperty("response", out var inner)
                && inner.TryGetProperty("models", out var models)
                && models.ValueKind == JsonValueKind.Array
                    ? [.. models.EnumerateArray().Select(Option).OfType<ModelOption>().Where(model => model.Id != Recommended)]
                    : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static ModelOption? Option(JsonElement model) =>
        model.ValueKind == JsonValueKind.Object && model.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String
            ? new ModelOption(
                value.GetString()!,
                model.TryGetProperty("displayName", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString()! : ModelName.Of(value.GetString()!))
            : null;

    private static IReadOnlyList<string> Listed(Match option, Regex value) =>
        option.Success ? [.. value.Matches(option.Groups[1].Value).Select(found => found.Groups[1].Value).Distinct()] : [];

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"--model <\w+> .*?\(e\.g\. (.*?)\)")]
    private static partial Regex Model();

    [GeneratedRegex(@"--effort <\w+> .*?\(([^)]*)\)")]
    private static partial Regex Effort();

    [GeneratedRegex(@"['""]([\w.-]+)['""]")]
    private static partial Regex Quoted();

    [GeneratedRegex(@"([\w.-]+)")]
    private static partial Regex Word();
}
