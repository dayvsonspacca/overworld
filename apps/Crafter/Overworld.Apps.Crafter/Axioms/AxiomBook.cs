using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Axioms;

public sealed class AxiomBook(Shelf shelf, SettingsStore settings)
{
    public async Task<Result<IReadOnlyList<string>>> ListAsync(string folder, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var saved = await settings.ReadAsync(found.Value, cancellationToken);

        return saved.IsSuccess ? Result.Success(saved.Value?.Axioms ?? []) : saved.Error;
    }

    public async Task<Result<IReadOnlyList<string>>> ChangeAsync(
        string folder,
        Func<IReadOnlyList<string>, Result<IReadOnlyList<string>>> change,
        CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        Result<IReadOnlyList<string>>? changed = null;

        var written = await settings.UpdateAsync(
            found.Value,
            saved =>
            {
                var held = saved?.Axioms ?? [];
                changed = change(held);

                if (!changed.IsSuccess)
                {
                    return changed.Error;
                }

                var axioms = changed.Value.Count > 0 ? changed.Value : null;

                return Result.Success<ProjectSettings?>(saved is null
                    ? new ProjectSettings(Project.Of(found.Value, null).Name, Axioms: axioms)
                    : saved with { Axioms = axioms });
            },
            cancellationToken);

        return written.IsSuccess ? changed! : written.Error;
    }
}
