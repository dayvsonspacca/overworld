namespace Overworld.Apps.Crafter.Presentation.Composing;

public sealed record NamePart(string Text, bool IsHit)
{
    public static IReadOnlyList<NamePart> Split(string name, IEnumerable<int> hits)
    {
        var lit = hits.ToHashSet();
        List<NamePart> parts = [];

        for (var at = 0; at < name.Length;)
        {
            var hit = lit.Contains(at);
            var end = at;

            while (end < name.Length && lit.Contains(end) == hit)
            {
                end++;
            }

            parts.Add(new NamePart(name[at..end], hit));
            at = end;
        }

        return parts;
    }
}
