using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Mcp;

public sealed class OperationTool(Operation operation) : McpServerTool
{
    public override Tool ProtocolTool { get; } = new()
    {
        Name = operation.Name,
        Description = operation.Description,
        InputSchema = InputSchema(operation.InputType),
    };

    public override IReadOnlyList<object> Metadata => [];

    public override async ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        var arguments = JsonSerializer.SerializeToNode(request.Params?.Arguments ?? new Dictionary<string, JsonElement>());

        var input = BedrockJson.ReadInput(arguments, operation.InputType);

        if (!input.IsSuccess)
        {
            return Failure(input.Error);
        }

        var result = await operation.ExecuteAsync(input.Value, cancellationToken);

        return result.IsSuccess
            ? Text(JsonSerializer.Serialize(result.Value, BedrockJson.Options))
            : Failure(result.Error);
    }

    private static JsonElement InputSchema(Type inputType)
    {
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(BedrockJson.Options, inputType, new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, node) =>
            {
                var description = context.PropertyInfo?.AttributeProvider?
                    .GetCustomAttributes(typeof(DescriptionAttribute), inherit: false)
                    .OfType<DescriptionAttribute>()
                    .FirstOrDefault()?
                    .Description;

                if (description is not null && node is JsonObject property)
                {
                    property.Insert(0, "description", description);
                }

                return node;
            },
        });

        return JsonSerializer.SerializeToElement(schema);
    }

    private static CallToolResult Text(string text) => new() { Content = [new TextContentBlock { Text = text }] };

    private static CallToolResult Failure(Error error)
    {
        var result = Text($"{error.Code}: {error.Message}");
        result.IsError = true;

        return result;
    }
}
