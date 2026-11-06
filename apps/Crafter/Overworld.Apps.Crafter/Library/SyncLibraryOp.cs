using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

[Operation("sync-library", "Brings the skills library to what its repository holds now.")]
public sealed class SyncLibraryOp(SkillLibrary library) : IOperation<SyncLibrary, LibraryState>
{
    public Task<Result<LibraryState>> ExecuteAsync(SyncLibrary input, CancellationToken cancellationToken) =>
        library.SyncAsync(cancellationToken);
}
