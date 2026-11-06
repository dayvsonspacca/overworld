namespace Overworld.Apps.Crafter.Agents;

public sealed record Blocked(string State, string Detail, string? Remedy) : Readiness(State, Detail);
