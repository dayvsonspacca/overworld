namespace Overworld.Desktop.Commands;

public sealed record PaletteItem(Command Command, IReadOnlyList<int> Hits)
{
    public static PaletteItem? Of(Command command, string query)
    {
        if (query.Length == 0)
        {
            return new PaletteItem(command, []);
        }

        var text = command.Title;
        List<int> hits = new(query.Length);
        var from = 0;

        foreach (var letter in query.Where(letter => !char.IsWhiteSpace(letter)))
        {
            var at = text.IndexOf(letter.ToString(), from, StringComparison.OrdinalIgnoreCase);

            if (at < 0)
            {
                return null;
            }

            hits.Add(at);
            from = at + 1;
        }

        return new PaletteItem(command, hits);
    }

    public int Score => Hits.Count == 0 ? 0 : (Hits[^1] - Hits[0]) + Hits[0];

    public bool Equals(PaletteItem? other) => other is not null && Command == other.Command && Hits.SequenceEqual(other.Hits);

    public override int GetHashCode() => HashCode.Combine(Command, Hits.Count);
}
