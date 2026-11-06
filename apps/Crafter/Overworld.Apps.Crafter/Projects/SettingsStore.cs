using System.Collections.Concurrent;
using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Crafter.Projects;

public sealed class SettingsStore
{
    private readonly ConcurrentDictionary<string, JsonFile<ProjectSettings?>> _files = new();

    public Task<Result<ProjectSettings?>> ReadAsync(string folder, CancellationToken cancellationToken) =>
        FileOf(folder).ReadAsync(cancellationToken);

    public Task<Result<ProjectSettings?>> UpdateAsync(
        string folder,
        Func<ProjectSettings?, Result<ProjectSettings?>> change,
        CancellationToken cancellationToken) =>
        FileOf(folder).UpdateAsync(change, cancellationToken);

    private JsonFile<ProjectSettings?> FileOf(string folder) =>
        _files.GetOrAdd(folder, key => new JsonFile<ProjectSettings?>(Path.Combine(key, ".crafter", "settings.json"), () => null));
}
