using System.Text.Json.Nodes;

namespace Overworld.Web;

public sealed record PageEvent(string Name, JsonNode? Data);
