using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

[Operation("set-default-skill", "Makes a library skill one every project gets, or stops it being one. The choice is this machine's.")]
public sealed class SetDefaultSkillOp(SkillLibrary library) : IOperation<SetDefaultSkill, LibraryState>
{
    public Task<Result<LibraryState>> ExecuteAsync(SetDefaultSkill input, CancellationToken cancellationToken) =>
        library.MakeDefaultAsync(input.Id, input.IsDefault, cancellationToken);
}
