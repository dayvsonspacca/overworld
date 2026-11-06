namespace Overworld.Apps.Crafter.Agents;

public sealed record Ready(string State, string Detail) : Readiness(State, Detail);
