using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Projects;

[Operation("remove-project", "Takes a project off the ones open in Crafter. Its folder and its .crafter settings stay as they are.")]
public sealed class RemoveProjectOp(Shelf shelf, SettingsStore settings) : IOperation<RemoveProject, Project>
{
    public async Task<Result<Project>> ExecuteAsync(RemoveProject input, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(input.Folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var folder = found.Value;
        var removed = await shelf.RemoveAsync(folder, cancellationToken);

        if (!removed.IsSuccess)
        {
            return removed.Error;
        }

        var described = await settings.ReadAsync(folder, cancellationToken);

        return Project.Of(folder, described.IsSuccess ? described.Value : null);
    }
}
