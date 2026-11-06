using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Projects;

[Operation("remove-project", "Takes a project off the ones open in Crafter. Its folder and its .crafter settings stay as they are.")]
public sealed class RemoveProjectOp(Shelf shelf, SettingsStore settings) : IOperation<RemoveProject, Project>
{
    public async Task<Result<Project>> ExecuteAsync(RemoveProject input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Folder))
        {
            return NotOpen(input.Folder);
        }

        var folder = Project.Normalize(input.Folder);
        var shelved = await shelf.ListAsync(cancellationToken);

        if (!shelved.IsSuccess)
        {
            return shelved.Error;
        }

        if (!shelved.Value.Contains(new Shelved(folder)))
        {
            return NotOpen(input.Folder);
        }

        var removed = await shelf.RemoveAsync(folder, cancellationToken);

        if (!removed.IsSuccess)
        {
            return removed.Error;
        }

        var described = await settings.ReadAsync(folder, cancellationToken);

        return Project.Of(folder, described.IsSuccess ? described.Value : null);
    }

    private static Error NotOpen(string folder) =>
        new("project.not_open", $"'{folder}' is not a project open in Crafter.");
}
