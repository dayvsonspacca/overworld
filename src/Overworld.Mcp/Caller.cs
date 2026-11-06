namespace Overworld.Mcp;

public sealed record Caller(string Name, IReadOnlyList<ServedTool> Tools, string? Instructions = null);
