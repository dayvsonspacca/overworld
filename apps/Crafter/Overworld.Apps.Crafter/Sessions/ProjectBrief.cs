using Overworld.Allay.Briefs;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Sessions;

public sealed class ProjectBrief(SummaryStore summaries, AxiomBook axioms, ProjectSkills skills)
{
    public async Task<Result<Brief>> OfAsync(string folder, IReadOnlyList<ToolServer> tools, CancellationToken cancellationToken)
    {
        var rules = await axioms.ListAsync(folder, cancellationToken);

        if (!rules.IsSuccess)
        {
            return rules.Error;
        }

        var summary = await summaries.ReadAsync(folder, cancellationToken);

        if (!summary.IsSuccess)
        {
            return summary.Error;
        }

        var given = await skills.OfAsync(folder, cancellationToken);

        if (!given.IsSuccess)
        {
            return given.Error;
        }

        return new Brief(folder, summary.Value.Text, rules.Value, [.. given.Value.Select(Briefed)], tools);
    }

    private static Allay.Briefs.Skill Briefed(Library.Skill skill) =>
        new(skill.Id, skill.Name, skill.WhenToUse is { } when ? $"{skill.Description} Use it when: {when}" : skill.Description, skill.Instructions);
}
