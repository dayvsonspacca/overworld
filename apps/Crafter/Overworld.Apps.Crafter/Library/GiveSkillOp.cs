using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

[Operation("give-skill", "Gives a library skill to one project, or takes it back, and lists the skills the project gets.")]
public sealed class GiveSkillOp(ProjectSkills skills) : IOperation<GiveSkill, IReadOnlyList<Skill>>
{
    public Task<Result<IReadOnlyList<Skill>>> ExecuteAsync(GiveSkill input, CancellationToken cancellationToken) =>
        skills.GiveAsync(input.Folder, input.Skill, input.Given, cancellationToken);
}
