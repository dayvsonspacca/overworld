using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Projects;

[Operation("list-projects", "Lists the projects open in Crafter, with their names and icons, in the order they were added.")]
public sealed class ListProjectsOp(Shelf shelf, SettingsStore settings) : IOperation<ListProjects, IReadOnlyList<Project>>
{
    public async Task<Result<IReadOnlyList<Project>>> ExecuteAsync(ListProjects input, CancellationToken cancellationToken)
    {
        var shelved = await shelf.ListAsync(cancellationToken);

        if (!shelved.IsSuccess)
        {
            return shelved.Error;
        }

        List<Project> projects = [];

        foreach (var entry in shelved.Value)
        {
            var read = await settings.ReadAsync(entry.Folder, cancellationToken);

            projects.Add(Project.Of(entry.Folder, read.IsSuccess ? read.Value : null));
        }

        return Result.Success<IReadOnlyList<Project>>(projects);
    }
}
