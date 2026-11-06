namespace Overworld.Allay.Settings;

public sealed record Choice : Setting
{
    public Choice(string key, string title, string about, IReadOnlyList<Alternative> alternatives)
        : base(key, title, about)
    {
        if (alternatives.Count == 0)
        {
            throw new ArgumentException("A choice needs at least one alternative.", nameof(alternatives));
        }

        Alternatives = alternatives;
    }

    public IReadOnlyList<Alternative> Alternatives { get; }

    public Alternative Chosen(Configuration configuration) =>
        Alternatives.FirstOrDefault(alternative => alternative.Value == configuration[Key]) ?? Alternatives[0];

    public bool Equals(Choice? other) => base.Equals(other) && Alternatives.SequenceEqual(other.Alternatives);

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Alternatives.Count);
}
