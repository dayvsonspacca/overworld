namespace Overworld.Allay.Metering;

public sealed record Usage(IReadOnlyList<Limit> Limits, Spending? Spending = null)
{
    public bool Equals(Usage? other) =>
        other is not null && Spending == other.Spending && Limits.SequenceEqual(other.Limits);

    public override int GetHashCode() => HashCode.Combine(Limits.Count, Spending);
}
