namespace Overworld.Allay.Briefs;

public sealed record ToolServer(string Name, Uri Endpoint, string Token, IReadOnlyList<string> FreeTools, TimeSpan? Patience = null)
{
    public bool Equals(ToolServer? other) =>
        other is not null && Name == other.Name && Endpoint == other.Endpoint && Token == other.Token && Patience == other.Patience
        && FreeTools.SequenceEqual(other.FreeTools);

    public override int GetHashCode() => HashCode.Combine(Name, Endpoint, Token, FreeTools.Count);
}
