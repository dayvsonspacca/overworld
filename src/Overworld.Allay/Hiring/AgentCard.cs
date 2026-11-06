using Overworld.Allay.Settings;

namespace Overworld.Allay.Hiring;

public sealed record AgentCard(
    string Id,
    string Name,
    string Summary,
    bool Hired,
    IReadOnlyList<Setting> Settings,
    IReadOnlyDictionary<string, string> Configuration)
{
    public bool Equals(AgentCard? other) =>
        other is not null && Id == other.Id && Name == other.Name && Summary == other.Summary && Hired == other.Hired
        && Settings.SequenceEqual(other.Settings) && Configuration.Count == other.Configuration.Count
        && Configuration.All(value => other.Configuration.GetValueOrDefault(value.Key) == value.Value);

    public override int GetHashCode() => HashCode.Combine(Id, Hired, Settings.Count, Configuration.Count);
}
