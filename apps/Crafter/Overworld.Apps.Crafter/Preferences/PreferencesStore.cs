using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Crafter.Preferences;

public sealed class PreferencesStore(string directory, string home)
{
    private readonly JsonFile<UserPreferences> _file = new(Path.Combine(directory, "preferences.json"), () => new UserPreferences());

    public async Task<Result<UserPreferences>> ReadAsync(CancellationToken cancellationToken)
    {
        var saved = await _file.ReadAsync(cancellationToken);

        return saved.IsSuccess ? Settled(saved.Value) : saved.Error;
    }

    public async Task<Result<UserPreferences>> UpdateAsync(Func<UserPreferences, Result<UserPreferences>> change, CancellationToken cancellationToken)
    {
        var changed = await _file.UpdateAsync(change, cancellationToken);

        return changed.IsSuccess ? Settled(changed.Value) : changed.Error;
    }

    private UserPreferences Settled(UserPreferences saved) => saved with { ProjectsRoot = saved.ProjectsRoot ?? home };
}
