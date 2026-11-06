using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Overworld.Apps.Crafter.Repositories;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Library;

public sealed partial class Mirror(string folder)
{
    private static readonly JsonSerializerOptions About = new(BedrockJson.Options)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<string>> SyncAsync(string repository, CancellationToken cancellationToken)
    {
        if (await IsCloneOfAsync(repository, cancellationToken))
        {
            var fetched = await Git.RunAsync(["-C", folder, "fetch", "--quiet", "--prune", "origin"], cancellationToken);

            if (!fetched.IsSuccess)
            {
                return fetched.Error;
            }

            var reset = await Git.RunAsync(["-C", folder, "reset", "--quiet", "--hard", "@{upstream}"], cancellationToken);

            if (!reset.IsSuccess)
            {
                return reset.Error;
            }
        }
        else
        {
            Clear();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(folder))!);

            var cloned = await Git.RunAsync(["clone", "--quiet", repository, folder], cancellationToken);

            if (!cloned.IsSuccess)
            {
                return cloned.Error;
            }
        }

        return await HeadAsync(cancellationToken);
    }

    public (IReadOnlyList<Skill> Skills, IReadOnlyList<SkillProblem> Problems) Read()
    {
        var skills = Path.Combine(folder, "skills");

        if (!Directory.Exists(skills))
        {
            return ([], []);
        }

        List<Skill> found = [];
        List<SkillProblem> problems = [];

        foreach (var place in Directory.GetDirectories(skills).Order(StringComparer.Ordinal))
        {
            var id = Path.GetFileName(place);
            var where = $"skills/{id}";

            if (Describe(place) is not { } about)
            {
                problems.Add(new SkillProblem(where, "skill.json is missing or not valid JSON"));
            }
            else if (string.IsNullOrWhiteSpace(about.Description))
            {
                problems.Add(new SkillProblem(where, "skill.json has no description"));
            }
            else if (!File.Exists(Path.Combine(place, "instructions.md")))
            {
                problems.Add(new SkillProblem(where, "instructions.md is missing"));
            }
            else
            {
                var name = string.IsNullOrWhiteSpace(about.Name) ? id : about.Name.Trim();
                var when = string.IsNullOrWhiteSpace(about.WhenToUse) ? null : about.WhenToUse.Trim();
                found.Add(new Skill(id, name, about.Description.Trim(), when, place));
            }
        }

        return ([.. found.OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)], problems);
    }

    public async Task<Result<string>> SaveAsync(SkillDraft skill, string repository, CancellationToken cancellationToken)
    {
        if (!SkillId().IsMatch(skill.Id))
        {
            return new Error("skill.id_invalid", "A skill's id is lowercase letters, digits and dashes, like laravel-migrations.");
        }

        if (string.IsNullOrWhiteSpace(skill.Description) || string.IsNullOrWhiteSpace(skill.Instructions))
        {
            return new Error("skill.incomplete", "A skill needs a description and instructions.");
        }

        var synced = await SyncAsync(repository, cancellationToken);

        if (!synced.IsSuccess)
        {
            return synced.Error;
        }

        var place = Path.Combine(folder, "skills", skill.Id);
        var known = Directory.Exists(place);
        var name = string.IsNullOrWhiteSpace(skill.Name) ? skill.Id : skill.Name.Trim();
        var about = new SkillAbout(name, skill.Description.Trim(), string.IsNullOrWhiteSpace(skill.WhenToUse) ? null : skill.WhenToUse.Trim());

        Directory.CreateDirectory(place);
        await File.WriteAllTextAsync(Path.Combine(place, "skill.json"), JsonSerializer.Serialize(about, About) + "\n", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(place, "instructions.md"), skill.Instructions.TrimEnd() + "\n", cancellationToken);

        var added = await Git.RunAsync(["-C", folder, "add", "--all", "--", $"skills/{skill.Id}"], cancellationToken);

        if (!added.IsSuccess)
        {
            return added.Error;
        }

        var unchanged = await Git.RunAsync(["-C", folder, "diff", "--cached", "--quiet"], cancellationToken);

        if (unchanged.IsSuccess)
        {
            return await HeadAsync(cancellationToken);
        }

        var committed = await Git.RunAsync(
            ["-C", folder, "commit", "--quiet", "-m", $"{(known ? "Update" : "Add")} skill {name}"],
            cancellationToken);

        if (!committed.IsSuccess)
        {
            return committed.Error;
        }

        var pushed = await Git.RunAsync(["-C", folder, "push", "--quiet", "origin", "HEAD"], cancellationToken);

        return pushed.IsSuccess ? await HeadAsync(cancellationToken) : pushed.Error;
    }

    private async Task<Result<string>> HeadAsync(CancellationToken cancellationToken)
    {
        var head = await Git.RunAsync(["-C", folder, "rev-parse", "--short", "HEAD"], cancellationToken);

        return head.IsSuccess ? head.Value.Trim() : head.Error;
    }

    private async Task<bool> IsCloneOfAsync(string repository, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(Path.Combine(folder, ".git")))
        {
            return false;
        }

        var origin = await Git.RunAsync(["-C", folder, "remote", "get-url", "origin"], cancellationToken);

        return origin.IsSuccess && origin.Value.Trim() == repository;
    }

    private void Clear()
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(folder, recursive: true);
    }

    private static SkillAbout? Describe(string place)
    {
        var file = Path.Combine(place, "skill.json");

        if (!File.Exists(file))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SkillAbout>(File.ReadAllText(file), About);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SkillId();
}
