namespace Overworld.Apps.Crafter.Presentation.Composing;

public static class FuzzyMatch
{
    public static IReadOnlyList<int>? Hits(string text, string query)
    {
        List<int> hits = [];
        var at = 0;

        foreach (var letter in query)
        {
            at = text.IndexOf(letter.ToString(), at, StringComparison.OrdinalIgnoreCase);

            if (at < 0)
            {
                return null;
            }

            hits.Add(at++);
        }

        return hits;
    }
}
