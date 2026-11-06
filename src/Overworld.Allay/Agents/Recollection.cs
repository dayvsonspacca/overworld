namespace Overworld.Allay.Agents;

public sealed record Recollection(IReadOnlyList<PastSession> Latest, int Total)
{
    public bool Equals(Recollection? other) => other is not null && Total == other.Total && Latest.SequenceEqual(other.Latest);

    public override int GetHashCode() => HashCode.Combine(Total, Latest.Count);
}
