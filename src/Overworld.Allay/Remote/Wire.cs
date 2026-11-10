using System.Text.Json;
using Overworld.Bedrock.Json;

namespace Overworld.Allay.Remote;

internal static class Wire
{
    private static readonly JsonSerializerOptions Options = new(BedrockJson.Options) { WriteIndented = false };

    public static string Write(Envelope envelope) => JsonSerializer.Serialize(envelope, Options);

    public static Envelope? Read(string line) => Read(line, out _);

    public static Envelope? Read(string line, out string? problem)
    {
        problem = null;

        try
        {
            return JsonSerializer.Deserialize<Envelope>(line, Options);
        }
        catch (JsonException unreadable)
        {
            problem = unreadable.Message;

            return null;
        }
    }
}
