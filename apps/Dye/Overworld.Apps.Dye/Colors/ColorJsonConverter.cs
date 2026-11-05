using System.Text.Json;
using System.Text.Json.Serialization;

namespace Overworld.Apps.Dye.Colors;

internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a hex color string, got {reader.TokenType}.");
        }

        var color = Color.Parse(reader.GetString()!);

        return color.IsSuccess ? color.Value : throw new JsonException(color.Error.Message);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Hex);
}
