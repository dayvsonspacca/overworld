namespace Overworld.Allay.Agents;

public sealed record Blocked(string State, string Detail, string? Remedy = null) : Readiness(State, Detail);
