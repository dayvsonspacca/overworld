using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

[Operation("link-library", "Links the skills library to a repository and syncs it, or unlinks it.")]
public sealed class LinkLibraryOp(SkillLibrary library) : IOperation<LinkLibrary, LibraryState>
{
    public Task<Result<LibraryState>> ExecuteAsync(LinkLibrary input, CancellationToken cancellationToken) =>
        library.LinkAsync(input.Repository, cancellationToken);
}
