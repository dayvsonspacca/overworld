namespace Overworld.Allay.Conversations;

public sealed record Questions(string Id, IReadOnlyList<Question> Asked) : Request(Id)
{
    public bool Equals(Questions? other) => other is not null && Id == other.Id && Asked.SequenceEqual(other.Asked);

    public override int GetHashCode() => HashCode.Combine(Id, Asked.Count);
}
