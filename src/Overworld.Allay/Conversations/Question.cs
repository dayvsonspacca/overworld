namespace Overworld.Allay.Conversations;

public sealed record Question(string Text, string Header, bool Several, IReadOnlyList<Option> Options)
{
    public bool Equals(Question? other) =>
        other is not null && Text == other.Text && Header == other.Header && Several == other.Several && Options.SequenceEqual(other.Options);

    public override int GetHashCode() => HashCode.Combine(Text, Header, Several, Options.Count);
}
