using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

[Operation("save-skill", "Writes a skill into the linked library, commits it and pushes it.")]
public sealed class SaveSkillOp(SkillLibrary library) : IOperation<SaveSkill, Skill>
{
    public Task<Result<Skill>> ExecuteAsync(SaveSkill input, CancellationToken cancellationToken) =>
        library.SaveAsync(new SkillDraft(input.Id, input.Name, input.Description, input.WhenToUse, input.Instructions), cancellationToken);
}
