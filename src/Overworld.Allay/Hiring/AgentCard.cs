using Overworld.Allay.Settings;

namespace Overworld.Allay.Hiring;

public sealed record AgentCard(string Id, string Name, string Summary, string Color, bool Hired, bool Metered, Setup Setup, IReadOnlyList<ModelOption> Models)
{
    public bool Equals(AgentCard? other) =>
        other is not null && Id == other.Id && Name == other.Name && Summary == other.Summary && Color == other.Color
        && Hired == other.Hired && Metered == other.Metered && Setup == other.Setup && Models.SequenceEqual(other.Models);

    public override int GetHashCode() => HashCode.Combine(Id, Hired, Setup, Models.Count);
}
