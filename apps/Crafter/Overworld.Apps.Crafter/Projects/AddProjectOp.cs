using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Projects;

[Operation("add-project", "Adds a folder to the projects open in Crafter, or renames a project already there.")]
public sealed class AddProjectOp(Shelf shelf, SettingsStore settings) : IOperation<AddProject, Project>
{
    public async Task<Result<Project>> ExecuteAsync(AddProject input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Folder) || !Directory.Exists(Project.Normalize(input.Folder)))
        {
            return new Error("project.folder_missing", $"There is no folder at '{input.Folder}'.");
        }

        var folder = Project.Normalize(input.Folder);
        var name = input.Name?.Trim();

        if (name is { Length: 0 })
        {
            return new Error("project.name_empty", "A project's name cannot be blank.");
        }

        var described = name is null
            ? await settings.ReadAsync(folder, cancellationToken)
            : await settings.UpdateAsync(folder, saved => Named(saved, name), cancellationToken);

        if (name is not null && !described.IsSuccess)
        {
            return described.Error;
        }

        var shelved = await shelf.AddAsync(folder, cancellationToken);

        if (!shelved.IsSuccess)
        {
            return shelved.Error;
        }

        return Project.Of(folder, described.IsSuccess ? described.Value : null);
    }

    private static Result<ProjectSettings?> Named(ProjectSettings? saved, string name) =>
        saved is null ? new ProjectSettings(name)
        : saved.Name == name ? saved
        : saved with { Name = name };
}
