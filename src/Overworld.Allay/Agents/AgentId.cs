namespace Overworld.Allay.Agents;

public sealed record AgentId(string Value)
{
    public override string ToString() => Value;
}
