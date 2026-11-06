using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Projects;

[Operation("edit-project", "Renames a project open in Crafter, or gives it an icon or takes its icon away.")]
public sealed class EditProjectOp(Shelf shelf, SettingsStore settings) : IOperation<EditProject, Project>
{
    public async Task<Result<Project>> ExecuteAsync(EditProject input, CancellationToken cancellationToken)
    {
        var name = input.Name?.Trim();

        if (name is { Length: 0 })
        {
            return new Error("project.name_empty", "A project's name cannot be blank.");
        }

        if (input.Icon is not null && input.RemoveIcon)
        {
            return new Error("project.icon_conflict", "Give the project an icon or take its icon away, not both.");
        }

        var found = await shelf.FindAsync(input.Folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var folder = found.Value;
        var saved = await settings.ReadAsync(folder, cancellationToken);

        if (!saved.IsSuccess)
        {
            return saved.Error;
        }

        var current = Project.Of(folder, saved.Value);
        var icon = current.Icon;

        if (input.Icon is not null)
        {
            var adopted = Icons.Adopt(folder, input.Icon);

            if (!adopted.IsSuccess)
            {
                return adopted.Error;
            }

            icon = adopted.Value;
        }
        else if (input.RemoveIcon)
        {
            Icons.Drop(folder);
            icon = null;
        }

        var edited = current with { Name = name ?? current.Name, Icon = icon };

        if (edited == current)
        {
            return current;
        }

        var written = await settings.UpdateAsync(
            folder,
            latest => Result.Success<ProjectSettings?>(
                (latest ?? new ProjectSettings(edited.Name)) with { Name = edited.Name, Icon = edited.Icon }),
            cancellationToken);

        return written.IsSuccess ? Project.Of(folder, written.Value) : written.Error;
    }
}
