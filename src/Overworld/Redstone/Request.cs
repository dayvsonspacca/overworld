using System.Text.Json.Nodes;

namespace Overworld.Redstone;

internal sealed record Request(string Operation, JsonNode? Input = null);
