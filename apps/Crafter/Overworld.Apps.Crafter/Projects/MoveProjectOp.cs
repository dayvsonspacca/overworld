using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Projects;

[Operation("move-project", "Moves a project open in Crafter to another place on the rail.")]
public sealed class MoveProjectOp(Shelf shelf, SettingsStore settings) : IOperation<MoveProject, Project>
{
    public async Task<Result<Project>> ExecuteAsync(MoveProject input, CancellationToken cancellationToken)
    {
        if (input.To < 0)
        {
            return new Error("project.place_invalid", "A project's place on the rail counts from 0.");
        }

        var found = await shelf.FindAsync(input.Folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var folder = found.Value;
        var moved = await shelf.MoveAsync(folder, input.To, cancellationToken);

        if (!moved.IsSuccess)
        {
            return moved.Error;
        }

        var described = await settings.ReadAsync(folder, cancellationToken);

        return Project.Of(folder, described.IsSuccess ? described.Value : null);
    }
}
