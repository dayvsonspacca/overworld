namespace Overworld.Allay.Conversations;

public sealed record Prompt(string Text, IReadOnlyList<Attachment> Attachments, IReadOnlyList<string> Skills) : Turn
{
    public bool Equals(Prompt? other) =>
        other is not null && Text == other.Text && Attachments.SequenceEqual(other.Attachments) && Skills.SequenceEqual(other.Skills);

    public override int GetHashCode() => HashCode.Combine(Text, Attachments.Count, Skills.Count);
}
