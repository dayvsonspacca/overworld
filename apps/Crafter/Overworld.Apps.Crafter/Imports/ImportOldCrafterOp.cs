using System.Text.Json.Nodes;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Imports;

[Operation(
    "import-old-crafter",
    "Copies the projects, preferences, chats and skill library of the Crafter that lived outside Overworld, and hires its agents in Allay with their setups. Nothing already here is replaced.")]
public sealed class ImportOldCrafterOp(OldCrafter old, string data, StaffFile staff) : IOperation<ImportOldCrafter, IReadOnlyList<string>>
{
    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(ImportOldCrafter input, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(old.Config) && !Directory.Exists(old.Data))
        {
            return new Error("import.nothing", $"There is no old Crafter here: neither {old.Config} nor {old.Data} exists.");
        }

        List<string> done = [];

        try
        {
            Directory.CreateDirectory(data);
            done.Add(Copy(Path.Combine(old.Config, "projects.json"), Path.Combine(data, "projects.json"), "projects"));
            done.Add(Copy(Path.Combine(old.Config, "preferences.json"), Path.Combine(data, "preferences.json"), "preferences"));
            done.Add(Chats(Path.Combine(old.Data, "chats"), Path.Combine(data, "chats")));
            done.Add(Library(Path.Combine(old.Data, "library"), Path.Combine(data, "library")));
        }
        catch (Exception failed) when (failed is IOException or UnauthorizedAccessException)
        {
            return new Error("import.failed", $"Could not copy the old Crafter: {failed.Message}");
        }

        var hired = await HireAsync(Path.Combine(old.Config, "preferences.json"), cancellationToken);

        if (!hired.IsSuccess)
        {
            return hired.Error;
        }

        done.Add(hired.Value);

        return done;
    }

    private static string Copy(string from, string to, string what)
    {
        if (!File.Exists(from))
        {
            return $"no {what} to copy";
        }

        if (File.Exists(to))
        {
            return $"kept the {what} already here";
        }

        File.Copy(from, to);

        return $"copied the {what}";
    }

    private static string Chats(string from, string to)
    {
        if (!Directory.Exists(from))
        {
            return "no chats to copy";
        }

        Directory.CreateDirectory(to);
        var copied = 0;

        foreach (var file in Directory.EnumerateFiles(from, "*.json"))
        {
            var target = Path.Combine(to, Path.GetFileName(file));

            if (!File.Exists(target))
            {
                File.Copy(file, target);
                copied++;
            }
        }

        return copied == 1 ? "copied the chats of 1 project" : $"copied the chats of {copied} projects";
    }

    private static string Library(string from, string to)
    {
        if (!Directory.Exists(from))
        {
            return "no skill library to copy";
        }

        if (Directory.Exists(to))
        {
            return "kept the skill library already here";
        }

        foreach (var folder in Directory.EnumerateDirectories(from, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, folder)));
        }

        Directory.CreateDirectory(to);

        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(to, Path.GetRelativePath(from, file)));
        }

        return "copied the skill library";
    }

    private async Task<Result<string>> HireAsync(string preferences, CancellationToken cancellationToken)
    {
        if (!File.Exists(preferences) || JsonNode.Parse(await File.ReadAllTextAsync(preferences, cancellationToken)) is not JsonObject read)
        {
            return "no agents to hire";
        }

        var agents = (read["agents"] as JsonArray)?.Select(agent => agent?.GetValue<string>()).OfType<string>().ToList() ?? [];
        var configurations = read["configurations"] as JsonObject;

        var written = await staff.UpdateAsync(
            saved =>
            {
                var hired = (saved.Hired ?? []).Union(agents).ToList();
                var setups = new Dictionary<string, Setup>(saved.Setups ?? new Dictionary<string, Setup>());

                foreach (var agent in agents.Where(agent => !setups.ContainsKey(agent)))
                {
                    if (SetupOf(configurations?[agent] as JsonObject) is { } setup && setup != Setup.Default)
                    {
                        setups[agent] = setup;
                    }
                }

                return saved with { Hired = hired.Count > 0 ? hired : null, Setups = setups.Count > 0 ? setups : null };
            },
            cancellationToken);

        return written.IsSuccess
            ? agents.Count == 0 ? "no agents to hire" : $"hired {string.Join(", ", agents)} in Allay"
            : written.Error;
    }

    private static Setup? SetupOf(JsonObject? configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        var mode = Text(configuration, "tools") switch
        {
            "acceptEdits" => Mode.Edits,
            "plan" => Mode.Plan,
            _ => Mode.Auto,
        };
        var model = Text(configuration, "model") is { Length: > 0 } chosen && chosen != Crew.DefaultModel ? chosen : null;
        var effort = Text(configuration, "effort") == "max" ? Effort.Max : Effort.Default;

        return new Setup(mode, model, effort);
    }

    private static string? Text(JsonObject configuration, string key) =>
        configuration[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
