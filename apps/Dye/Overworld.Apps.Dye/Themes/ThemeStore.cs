using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

public sealed partial class ThemeStore(string folder, string active)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public async Task<Result<IReadOnlyList<Theme>>> ListAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folder))
        {
            return Missing();
        }

        var current = Current();
        List<Theme> themes = [];

        foreach (var path in Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal))
        {
            var theme = await ReadAsync(path, current, cancellationToken);

            if (!theme.IsSuccess)
            {
                return theme.Error;
            }

            themes.Add(theme.Value);
        }

        return themes;
    }

    public async Task<Result<Theme>> FindAsync(string name, CancellationToken cancellationToken)
    {
        var path = Existing(name);

        return path.IsSuccess ? await ReadAsync(path.Value, Current(), cancellationToken) : path.Error;
    }

    public async Task<Result<Theme>> SaveAsync(
        string name,
        string from,
        IReadOnlyDictionary<string, string> colors,
        CancellationToken cancellationToken)
    {
        var target = PathOf(name);

        if (!target.IsSuccess)
        {
            return target.Error;
        }

        if (File.Exists(target.Value))
        {
            return new Error("theme.exists", $"A theme named '{name}' already exists.");
        }

        var source = Existing(from);

        if (!source.IsSuccess)
        {
            return source.Error;
        }

        var document = await ParseAsync(source.Value, cancellationToken);

        if (!document.IsSuccess)
        {
            return document.Error;
        }

        var palette = (JsonObject)document.Value["colors"]!;

        foreach (var (role, value) in colors)
        {
            if (!palette.ContainsKey(role))
            {
                return new Error(
                    "theme.unknown_role",
                    $"'{from}' has no color '{role}'; its colors are {string.Join(", ", palette.Select(color => color.Key))}.");
            }

            palette[role] = value;
        }

        await File.WriteAllTextAsync(target.Value, document.Value.ToJsonString(Indented) + "\n", cancellationToken);

        return await ReadAsync(target.Value, Current(), cancellationToken);
    }

    public async Task<Result<Theme>> ApplyAsync(string name, CancellationToken cancellationToken)
    {
        var path = Existing(name);

        if (!path.IsSuccess)
        {
            return path.Error;
        }

        var existing = new FileInfo(active);

        if (existing.LinkTarget is null && (existing.Exists || Directory.Exists(active)))
        {
            return new Error("theme.taken", $"{active} is not a link to a theme; move it away to apply one.");
        }

        Directory.CreateDirectory(existing.DirectoryName!);

        var swap = $"{active}.{Guid.NewGuid():N}";
        File.CreateSymbolicLink(swap, Path.Combine(RealFolder(), Path.GetFileName(path.Value)));
        File.Move(swap, active, overwrite: true);

        return await ReadAsync(path.Value, Current(), cancellationToken);
    }

    private Result<string> PathOf(string name) =>
        Name().IsMatch(name)
            ? Path.Combine(folder, $"{name}.json")
            : new Error("theme.name_invalid", $"'{name}' is not a theme name; use lowercase letters, digits and dashes, like deep-sea.");

    private Result<string> Existing(string name)
    {
        if (!Directory.Exists(folder))
        {
            return Missing();
        }

        var path = PathOf(name);

        return !path.IsSuccess || File.Exists(path.Value)
            ? path
            : new Error("theme.not_found", $"There is no theme named '{name}'.");
    }

    private string? Current()
    {
        var link = new FileInfo(active).LinkTarget;

        if (link is null)
        {
            return null;
        }

        var target = Path.GetFullPath(link, Path.GetDirectoryName(active)!);

        return Path.GetDirectoryName(target) == RealFolder() ? Path.GetFileNameWithoutExtension(target) : null;
    }

    private string RealFolder() =>
        Path.TrimEndingDirectorySeparator(
            new DirectoryInfo(folder).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? Path.GetFullPath(folder));

    private static async Task<Result<Theme>> ReadAsync(string path, string? current, CancellationToken cancellationToken)
    {
        var document = await ParseAsync(path, cancellationToken);

        if (!document.IsSuccess)
        {
            return document.Error;
        }

        var name = Path.GetFileNameWithoutExtension(path);
        var colors = ((JsonObject)document.Value["colors"]!).ToDictionary(
            color => color.Key,
            color => color.Value!.GetValue<string>());

        return new Theme(name, name == current, colors);
    }

    private static async Task<Result<JsonObject>> ParseAsync(string path, CancellationToken cancellationToken)
    {
        JsonNode? document;

        try
        {
            document = JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken));
        }
        catch (JsonException)
        {
            return Invalid(path);
        }

        return document is JsonObject theme
            && theme["colors"] is JsonObject colors
            && colors.All(color => color.Value?.GetValueKind() == JsonValueKind.String)
                ? theme
                : Invalid(path);
    }

    private static Error Invalid(string path) =>
        new("theme.invalid", $"{path} is not a theme: it needs a 'colors' object of hex colors.");

    private Error Missing() =>
        new("theme.folder_missing", $"There are no themes at {folder}; run just seed to link them.");

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Name();
}
