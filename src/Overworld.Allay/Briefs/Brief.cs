namespace Overworld.Allay.Briefs;

public sealed record Brief(
    string Folder,
    string? Context,
    IReadOnlyList<string> Rules,
    IReadOnlyList<Skill> Skills,
    IReadOnlyList<ToolServer> Tools)
{
    public BriefChange Since(Brief before) => new(
        [.. Rules.Except(before.Rules)],
        [.. before.Rules.Except(Rules)],
        Context == before.Context ? null : Context ?? string.Empty,
        [.. Skills.Except(before.Skills)],
        [.. before.Skills.Except(Skills)],
        !Tools.SequenceEqual(before.Tools));

    public bool Equals(Brief? other) =>
        other is not null && Folder == other.Folder && Context == other.Context
        && Rules.SequenceEqual(other.Rules) && Skills.SequenceEqual(other.Skills) && Tools.SequenceEqual(other.Tools);

    public override int GetHashCode() => HashCode.Combine(Folder, Context, Rules.Count, Skills.Count, Tools.Count);
}
