using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Preferences;

[Operation("show-preferences", "Shows Crafter's preferences, like the folder where the folder picker starts.")]
public sealed class ShowPreferencesOp(PreferencesStore store) : IOperation<ShowPreferences, UserPreferences>
{
    public Task<Result<UserPreferences>> ExecuteAsync(ShowPreferences input, CancellationToken cancellationToken) =>
        store.ReadAsync(cancellationToken);
}
