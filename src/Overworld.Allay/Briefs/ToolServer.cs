namespace Overworld.Allay.Briefs;

public sealed record ToolServer(
    string Name,
    Uri? Endpoint,
    string? Token,
    IReadOnlyList<string> FreeTools,
    TimeSpan? Patience = null,
    string? Command = null,
    IReadOnlyList<string>? Arguments = null)
{
    public static ToolServer Started(string name, string command, IReadOnlyList<string> arguments, IReadOnlyList<string> freeTools, TimeSpan? patience = null) =>
        new(name, null, null, freeTools, patience, command, arguments);

    public bool Equals(ToolServer? other) =>
        other is not null && Name == other.Name && Endpoint == other.Endpoint && Token == other.Token && Patience == other.Patience
        && Command == other.Command && (Arguments ?? []).SequenceEqual(other.Arguments ?? [])
        && FreeTools.SequenceEqual(other.FreeTools);

    public override int GetHashCode() => HashCode.Combine(Name, Endpoint, Token, Command, FreeTools.Count);
}
