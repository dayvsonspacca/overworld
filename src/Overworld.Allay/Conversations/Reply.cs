namespace Overworld.Allay.Conversations;

public sealed record Reply(IReadOnlyList<Moment> Moments, Outcome Outcome, TimeSpan? Took = null) : Turn
{
    public bool Equals(Reply? other) =>
        other is not null && Outcome == other.Outcome && Took == other.Took && Moments.SequenceEqual(other.Moments);

    public override int GetHashCode() => HashCode.Combine(Outcome, Moments.Count);
}
