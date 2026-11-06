namespace Overworld.Allay.Conversations;

public sealed record QuestionAnswer(Question Question, IReadOnlyList<Option> Picked, string? Written = null)
{
    public bool Equals(QuestionAnswer? other) =>
        other is not null && Question == other.Question && Written == other.Written && Picked.SequenceEqual(other.Picked);

    public override int GetHashCode() => HashCode.Combine(Question, Written, Picked.Count);
}
