using Overworld.Allay.Settings;

namespace Overworld.Apps.Crafter.Sessions;

public sealed record Desk(string Agent, string Color, Setup Setup, IReadOnlyList<ModelOption> Models)
{
    public bool Equals(Desk? other) => other is not null && Agent == other.Agent && Color == other.Color && Setup == other.Setup && Models.SequenceEqual(other.Models);

    public override int GetHashCode() => HashCode.Combine(Agent, Color, Setup, Models.Count);
}
