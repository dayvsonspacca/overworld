namespace Overworld.Allay.Conversations;

public sealed record Plan(IReadOnlyList<PlanStep> Steps) : Moment
{
    public bool Equals(Plan? other) => other is not null && Steps.SequenceEqual(other.Steps);

    public override int GetHashCode() => Steps.Count;
}
