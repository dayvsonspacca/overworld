namespace Overworld.Allay.Conversations;

public sealed record Answers(IReadOnlyList<QuestionAnswer> Given) : Decision
{
    public bool Equals(Answers? other) => other is not null && Given.SequenceEqual(other.Given);

    public override int GetHashCode() => Given.Count;
}
