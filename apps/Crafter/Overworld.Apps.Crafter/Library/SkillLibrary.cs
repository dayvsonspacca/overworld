using Overworld.Apps.Crafter.Preferences;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

public sealed class SkillLibrary(PreferencesStore preferences, Mirror mirror)
{
    public async Task<Result<LibraryState>> StateAsync(CancellationToken cancellationToken)
    {
        var preferred = await preferences.ReadAsync(cancellationToken);

        return preferred.IsSuccess ? StateOf(preferred.Value) : preferred.Error;
    }

    public async Task<Result<LibraryState>> LinkAsync(string? repository, CancellationToken cancellationToken)
    {
        var linked = repository?.Trim() is { Length: > 0 } trimmed ? trimmed : null;
        var preferred = await preferences.UpdateAsync(
            saved => saved.Library == linked ? saved : saved with { Library = linked },
            cancellationToken);

        if (!preferred.IsSuccess)
        {
            return preferred.Error;
        }

        return linked is null ? StateOf(preferred.Value) : await SyncAsync(cancellationToken);
    }

    public async Task<Result<LibraryState>> SyncAsync(CancellationToken cancellationToken)
    {
        var preferred = await preferences.ReadAsync(cancellationToken);

        if (!preferred.IsSuccess)
        {
            return preferred.Error;
        }

        if (preferred.Value.Library is not { } repository)
        {
            return Unlinked();
        }

        var synced = await mirror.SyncAsync(repository, cancellationToken);

        return synced.IsSuccess ? StateOf(preferred.Value) : synced.Error;
    }

    public async Task<Result<Skill>> SaveAsync(SkillDraft draft, CancellationToken cancellationToken)
    {
        var preferred = await preferences.ReadAsync(cancellationToken);

        if (!preferred.IsSuccess)
        {
            return preferred.Error;
        }

        if (preferred.Value.Library is not { } repository)
        {
            return Unlinked();
        }

        var saved = await mirror.SaveAsync(draft, repository, cancellationToken);

        if (!saved.IsSuccess)
        {
            return saved.Error;
        }

        return mirror.Read().Skills.First(skill => skill.Id == draft.Id);
    }

    public async Task<Result<LibraryState>> MakeDefaultAsync(string id, bool isDefault, CancellationToken cancellationToken)
    {
        var known = await FindAsync(id, cancellationToken);

        if (!known.IsSuccess)
        {
            return known.Error;
        }

        var preferred = await preferences.UpdateAsync(
            saved =>
            {
                var defaults = saved.DefaultSkills ?? [];

                if (defaults.Contains(id) == isDefault)
                {
                    return saved;
                }

                IReadOnlyList<string> changed = isDefault ? [.. defaults, id] : [.. defaults.Where(each => each != id)];

                return saved with { DefaultSkills = changed.Count > 0 ? changed : null };
            },
            cancellationToken);

        return preferred.IsSuccess ? StateOf(preferred.Value) : preferred.Error;
    }

    public async Task<Result<Skill>> FindAsync(string id, CancellationToken cancellationToken)
    {
        var state = await StateAsync(cancellationToken);

        if (!state.IsSuccess)
        {
            return state.Error;
        }

        return state.Value.Skills.FirstOrDefault(skill => skill.Id == id) is { } skill
            ? skill
            : new Error("skill.missing", $"The library has no skill '{id}'.");
    }

    private LibraryState StateOf(UserPreferences preferred)
    {
        var (skills, problems) = preferred.Library is null ? ([], []) : mirror.Read();

        return new LibraryState(preferred.Library, skills, problems, preferred.DefaultSkills ?? []);
    }

    private static Error Unlinked() =>
        new("library.unlinked", "No library is linked; link a repository with a skills/ folder first.");
}
