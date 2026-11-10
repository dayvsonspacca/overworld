using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Mcp;

public sealed class OperationTool(Operation operation, IReadOnlyDictionary<string, JsonNode>? presets = null) : McpServerTool
{
    public override Tool ProtocolTool { get; } = new()
    {
        Name = operation.Name,
        Description = operation.Description,
        InputSchema = JsonSerializer.SerializeToElement(OperationSchema.Of(operation.InputType, presets?.Keys.ToList())),
    };

    public override IReadOnlyList<object> Metadata => [];

    public override async ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        var arguments = JsonSerializer.SerializeToNode(request.Params?.Arguments ?? new Dictionary<string, JsonElement>()) as JsonObject ?? [];

        foreach (var (name, value) in presets ?? new Dictionary<string, JsonNode>())
        {
            arguments[name] = value.DeepClone();
        }

        var result = await operation.RunAsync(arguments, cancellationToken);

        return result.IsSuccess
            ? Text(result.Value?.ToJsonString(BedrockJson.Options) ?? "null")
            : Failure(result.Error);
    }

    private static CallToolResult Text(string text) => new() { Content = [new TextContentBlock { Text = text }] };

    private static CallToolResult Failure(Error error)
    {
        var result = Text($"{error.Code}: {error.Message}");
        result.IsError = true;

        return result;
    }
}
