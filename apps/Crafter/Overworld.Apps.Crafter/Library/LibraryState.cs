namespace Overworld.Apps.Crafter.Library;

public sealed record LibraryState(
    string? Repository,
    IReadOnlyList<Skill> Skills,
    IReadOnlyList<SkillProblem> Problems,
    IReadOnlyList<string> Defaults);
