using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class ThemeStep(string themes, string? theme, string target) : IStep
{
    private const string First = "amethyst";

    private const string Dark = "dark";

    public Task<Result<string>> RunAsync(CancellationToken cancellationToken)
    {
        var existing = new FileInfo(target);
        var worn = existing.LinkTarget is { } link ? Worn(Path.GetFullPath(link, existing.DirectoryName!)) : null;

        if (theme is null && worn is { Exists: true })
        {
            return Task.FromResult<Result<string>>($"{target} keeps {worn.Value.Name} {worn.Value.Variant}");
        }

        var source = Path.Combine(themes, theme ?? worn?.Name ?? First, $"{worn?.Variant ?? Dark}.json");

        if (!File.Exists(source))
        {
            var known = Directory.EnumerateDirectories(themes).Select(Path.GetFileName).Order();

            return Task.FromResult<Result<string>>(
                new Error("seed.unknown_theme", $"There is no theme '{theme}'; the themes are {string.Join(", ", known)}."));
        }

        if (worn is not null)
        {
            existing.Delete();
        }

        return new LinkStep(source, target).RunAsync(cancellationToken);
    }

    private (string Name, string Variant, bool Exists)? Worn(string path)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(themes));
        var folder = Path.GetDirectoryName(path);

        if (folder == root)
        {
            var name = Path.GetFileNameWithoutExtension(path);

            return (Directory.Exists(Path.Combine(root, name)) ? name : First, Dark, false);
        }

        return Path.GetDirectoryName(folder) == root
            ? (Path.GetFileName(folder)!, Path.GetFileNameWithoutExtension(path), File.Exists(path))
            : null;
    }
}
