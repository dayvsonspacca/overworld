using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Crafter.Projects;

public sealed class Shelf(string directory)
{
    private readonly JsonFile<IReadOnlyList<Shelved>> _file = new(Path.Combine(directory, "projects.json"), () => []);

    public Task<Result<IReadOnlyList<Shelved>>> ListAsync(CancellationToken cancellationToken) =>
        _file.ReadAsync(cancellationToken);

    public async Task<Result<string>> FindAsync(string folder, CancellationToken cancellationToken)
    {
        var shelf = await ListAsync(cancellationToken);

        if (!shelf.IsSuccess)
        {
            return shelf.Error;
        }

        var normalized = string.IsNullOrWhiteSpace(folder) ? null : Project.Normalize(folder);

        return normalized is not null && shelf.Value.Contains(new Shelved(normalized))
            ? normalized
            : new Error("project.not_open", $"'{folder}' is not a project open in Crafter.");
    }

    public Task<Result<IReadOnlyList<Shelved>>> AddAsync(string folder, CancellationToken cancellationToken) =>
        _file.UpdateAsync(
            shelf => shelf.Any(shelved => shelved.Folder == folder)
                ? Result.Success(shelf)
                : Result.Success<IReadOnlyList<Shelved>>([.. shelf, new Shelved(folder)]),
            cancellationToken);

    public Task<Result<IReadOnlyList<Shelved>>> RemoveAsync(string folder, CancellationToken cancellationToken) =>
        _file.UpdateAsync(
            shelf => shelf.Any(shelved => shelved.Folder == folder)
                ? Result.Success<IReadOnlyList<Shelved>>([.. shelf.Where(shelved => shelved.Folder != folder)])
                : Result.Success(shelf),
            cancellationToken);
}
