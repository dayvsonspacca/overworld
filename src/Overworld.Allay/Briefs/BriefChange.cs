namespace Overworld.Allay.Briefs;

public sealed record BriefChange(
    IReadOnlyList<string> AddedRules,
    IReadOnlyList<string> RemovedRules,
    string? NewContext,
    IReadOnlyList<Skill> AddedSkills,
    IReadOnlyList<Skill> RemovedSkills,
    bool ToolsChanged)
{
    public bool IsEmpty =>
        AddedRules.Count == 0 && RemovedRules.Count == 0 && NewContext is null
        && AddedSkills.Count == 0 && RemovedSkills.Count == 0 && !ToolsChanged;

    public bool Equals(BriefChange? other) =>
        other is not null && NewContext == other.NewContext && ToolsChanged == other.ToolsChanged
        && AddedRules.SequenceEqual(other.AddedRules) && RemovedRules.SequenceEqual(other.RemovedRules)
        && AddedSkills.SequenceEqual(other.AddedSkills) && RemovedSkills.SequenceEqual(other.RemovedSkills);

    public override int GetHashCode() => HashCode.Combine(NewContext, ToolsChanged, AddedRules.Count, RemovedRules.Count);
}
