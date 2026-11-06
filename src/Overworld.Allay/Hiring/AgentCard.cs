using Overworld.Allay.Settings;

namespace Overworld.Allay.Hiring;

public sealed record AgentCard(string Id, string Name, string Summary, bool Hired, Setup Setup, IReadOnlyList<ModelOption> Models)
{
    public bool Equals(AgentCard? other) =>
        other is not null && Id == other.Id && Name == other.Name && Summary == other.Summary && Hired == other.Hired
        && Setup == other.Setup && Models.SequenceEqual(other.Models);

    public override int GetHashCode() => HashCode.Combine(Id, Hired, Setup, Models.Count);
}
