using System.Text.Json;
using System.Text.Json.Nodes;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Json;

public static class BedrockJson
{
    public const string InvalidInput = "input.invalid";

    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static Result<object> ReadInput(JsonNode? json, Type inputType)
    {
        try
        {
            return JsonSerializer.Deserialize(json, inputType, Options) is { } input
                ? Result.Success<object>(input)
                : new Error(InvalidInput, "The input must be an object, not null.");
        }
        catch (JsonException exception)
        {
            return new Error(InvalidInput, exception.Message);
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            WriteIndented = true,
        };

        options.MakeReadOnly(populateMissingResolver: true);

        return options;
    }
}
