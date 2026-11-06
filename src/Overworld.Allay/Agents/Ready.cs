namespace Overworld.Allay.Agents;

public sealed record Ready(string State, string Detail) : Readiness(State, Detail);
