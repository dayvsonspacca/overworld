using System.Text.Json.Nodes;
using Overworld.Bedrock.Operations;

namespace Overworld.Mcp;

public sealed record ServedTool(Operation Operation, IReadOnlyDictionary<string, JsonNode>? Presets = null)
{
    public string Name => Operation.Name;

    public JsonObject Describe() => new()
    {
        ["name"] = Operation.Name,
        ["description"] = Operation.Description,
        ["inputSchema"] = OperationSchema.Of(Operation.InputType, Presets?.Keys.ToList()),
    };

    public JsonObject Arguments(JsonObject? sent)
    {
        var arguments = sent?.DeepClone().AsObject() ?? [];

        foreach (var (name, value) in Presets ?? new Dictionary<string, JsonNode>())
        {
            arguments[name] = value.DeepClone();
        }

        return arguments;
    }
}
