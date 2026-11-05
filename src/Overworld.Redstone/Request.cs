using System.Text.Json.Nodes;

namespace Overworld.Redstone;

public sealed record Request(string Operation, JsonNode? Input = null);
