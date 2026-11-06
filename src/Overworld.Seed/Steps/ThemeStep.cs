using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class ThemeStep(string themes, string? theme, string target) : IStep
{
    private const string First = "amethyst";

    public Task<Result<string>> RunAsync(CancellationToken cancellationToken)
    {
        var existing = new FileInfo(target);
        var worn = existing.LinkTarget is { } link ? Path.GetFullPath(link, existing.DirectoryName!) : null;
        var wearsTheme = worn is not null && IsTheme(worn);

        if (theme is null && wearsTheme && File.Exists(worn))
        {
            return Task.FromResult<Result<string>>($"{target} keeps {Path.GetFileNameWithoutExtension(worn)}");
        }

        var source = Path.Combine(themes, $"{theme ?? First}.json");

        if (!File.Exists(source))
        {
            var known = Directory.EnumerateFiles(themes, "*.json").Select(Path.GetFileNameWithoutExtension).Order();

            return Task.FromResult<Result<string>>(
                new Error("seed.unknown_theme", $"There is no theme '{theme}'; the themes are {string.Join(", ", known)}."));
        }

        if (wearsTheme)
        {
            existing.Delete();
        }

        return new LinkStep(source, target).RunAsync(cancellationToken);
    }

    private bool IsTheme(string path) =>
        Path.GetDirectoryName(path) == Path.TrimEndingDirectorySeparator(Path.GetFullPath(themes));
}
