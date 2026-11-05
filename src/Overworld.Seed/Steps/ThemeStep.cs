using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class ThemeStep(string themes, string theme, string target) : IStep
{
    public Task<Result<string>> RunAsync(CancellationToken cancellationToken)
    {
        var source = Path.Combine(themes, $"{theme}.json");

        if (!File.Exists(source))
        {
            var known = Directory.EnumerateFiles(themes, "*.json").Select(Path.GetFileNameWithoutExtension).Order();

            return Task.FromResult<Result<string>>(
                new Error("seed.unknown_theme", $"There is no theme '{theme}'; the themes are {string.Join(", ", known)}."));
        }

        var existing = new FileInfo(target);

        if (existing.LinkTarget is { } link && IsTheme(Path.GetFullPath(link, existing.DirectoryName!)))
        {
            existing.Delete();
        }

        return new LinkStep(source, target).RunAsync(cancellationToken);
    }

    private bool IsTheme(string path) =>
        Path.GetDirectoryName(path) == Path.TrimEndingDirectorySeparator(Path.GetFullPath(themes));
}
