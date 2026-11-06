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

        return normalized is not null && shelf.Value.Any(shelved => shelved.Folder == normalized)
            ? normalized
            : new Error("project.not_open", $"'{folder}' is not a project open in Crafter.");
    }

    public Task<Result<IReadOnlyList<Shelved>>> AddAsync(string folder, CancellationToken cancellationToken) =>
        _file.UpdateAsync(
            shelf => shelf.Any(shelved => shelved.Folder == folder)
                ? Result.Success(shelf)
                : Result.Success<IReadOnlyList<Shelved>>([.. shelf, new Shelved(folder)]),
            cancellationToken);

    public async Task<Result<IReadOnlyList<string>>> GiveAsync(string folder, string skill, bool given, CancellationToken cancellationToken)
    {
        var updated = await _file.UpdateAsync(
            shelf =>
            {
                var place = shelf.ToList().FindIndex(shelved => shelved.Folder == folder);

                if (place < 0)
                {
                    return new Error("project.not_open", $"'{folder}' is not a project open in Crafter.");
                }

                var skills = shelf[place].Skills ?? [];

                if (skills.Contains(skill) == given)
                {
                    return Result.Success(shelf);
                }

                IReadOnlyList<string> changed = given ? [.. skills, skill] : [.. skills.Where(each => each != skill)];
                var entries = shelf.ToList();
                entries[place] = entries[place] with { Skills = changed.Count > 0 ? changed : null };

                return Result.Success<IReadOnlyList<Shelved>>(entries);
            },
            cancellationToken);

        return updated.IsSuccess
            ? Result.Success(updated.Value.First(shelved => shelved.Folder == folder).Skills ?? [])
            : updated.Error;
    }

    public Task<Result<IReadOnlyList<Shelved>>> RemoveAsync(string folder, CancellationToken cancellationToken) =>
        _file.UpdateAsync(
            shelf => shelf.Any(shelved => shelved.Folder == folder)
                ? Result.Success<IReadOnlyList<Shelved>>([.. shelf.Where(shelved => shelved.Folder != folder)])
                : Result.Success(shelf),
            cancellationToken);
}
