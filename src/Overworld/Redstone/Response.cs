using System.Text.Json.Nodes;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone;

internal sealed record Response(JsonNode? Result = null, Error? Error = null)
{
    public static Response Success(JsonNode? result) => new(Result: result);

    public static Response Failure(Error error) => new(Error: error);
}
