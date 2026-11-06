using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Preferences;

[Operation("set-projects-root", "Moves the folder where the folder picker starts and new projects are made.")]
public sealed class SetProjectsRootOp(PreferencesStore store) : IOperation<SetProjectsRoot, UserPreferences>
{
    public Task<Result<UserPreferences>> ExecuteAsync(SetProjectsRoot input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Folder) || !Directory.Exists(Project.Normalize(input.Folder)))
        {
            return Task.FromResult<Result<UserPreferences>>(new Error("preferences.folder_missing", $"There is no folder at '{input.Folder}'."));
        }

        var folder = Project.Normalize(input.Folder);

        return store.UpdateAsync(
            saved => saved.ProjectsRoot == folder ? saved : saved with { ProjectsRoot = folder },
            cancellationToken);
    }
}
