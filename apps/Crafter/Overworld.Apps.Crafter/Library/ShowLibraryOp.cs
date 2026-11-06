using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

[Operation("show-library", "Shows the linked skills library: its repository, its skills, the folders it could not read and the skills every project gets.")]
public sealed class ShowLibraryOp(SkillLibrary library) : IOperation<ShowLibrary, LibraryState>
{
    public Task<Result<LibraryState>> ExecuteAsync(ShowLibrary input, CancellationToken cancellationToken) =>
        library.StateAsync(cancellationToken);
}
