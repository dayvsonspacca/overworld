using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

[Operation("list-project-skills", "Lists the library skills a project gets: the ones every project gets and the ones given to it.")]
public sealed class ListProjectSkillsOp(ProjectSkills skills) : IOperation<ListProjectSkills, IReadOnlyList<Skill>>
{
    public Task<Result<IReadOnlyList<Skill>>> ExecuteAsync(ListProjectSkills input, CancellationToken cancellationToken) =>
        skills.OfAsync(input.Folder, cancellationToken);
}
