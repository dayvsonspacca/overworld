using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Overworld.Bedrock.Json;

namespace Overworld.Mcp;

public static class OperationSchema
{
    public static JsonObject Of(Type inputType, IReadOnlyCollection<string>? hidden = null)
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
        }).AsObject();

        foreach (var name in hidden ?? [])
        {
            (schema["properties"] as JsonObject)?.Remove(name);

            if (schema["required"] is JsonArray required && required.FirstOrDefault(each => each?.GetValue<string>() == name) is { } listed)
            {
                required.Remove(listed);
            }
        }

        return schema;
    }
}
