using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

public sealed class ProjectSkills(Shelf shelf, SkillLibrary library)
{
    public async Task<Result<IReadOnlyList<Skill>>> OfAsync(string folder, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var listed = await shelf.ListAsync(cancellationToken);

        if (!listed.IsSuccess)
        {
            return listed.Error;
        }

        var given = listed.Value.First(shelved => shelved.Folder == found.Value).Skills ?? [];

        return await GottenAsync(given, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Skill>>> GiveAsync(string folder, string id, bool given, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var known = await library.FindAsync(id, cancellationToken);

        if (!known.IsSuccess)
        {
            return known.Error;
        }

        var skills = await shelf.GiveAsync(found.Value, id, given, cancellationToken);

        return skills.IsSuccess ? await GottenAsync(skills.Value, cancellationToken) : skills.Error;
    }

    private async Task<Result<IReadOnlyList<Skill>>> GottenAsync(IReadOnlyList<string> given, CancellationToken cancellationToken)
    {
        var state = await library.StateAsync(cancellationToken);

        return state.IsSuccess
            ? Result.Success<IReadOnlyList<Skill>>([.. state.Value.Skills.Where(skill => state.Value.Defaults.Contains(skill.Id) || given.Contains(skill.Id))])
            : state.Error;
    }
}
