using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class LinkStep(string source, string target) : IStep
{
    public Task<Result<string>> RunAsync(CancellationToken cancellationToken) => Task.FromResult(Link());

    private Result<string> Link()
    {
        if (!Path.Exists(source))
        {
            return new Error("seed.missing_source", $"{source} does not exist, so there is nothing to link at {target}.");
        }

        var existing = new FileInfo(target);

        if (existing.LinkTarget is { } link)
        {
            return Same(Path.GetFullPath(link, existing.DirectoryName!), source)
                ? $"{target} already points to {source}"
                : Taken($"a link to {link}");
        }

        if (Directory.Exists(target))
        {
            return Taken("a folder");
        }

        if (File.Exists(target))
        {
            return Taken("a file");
        }

        Directory.CreateDirectory(existing.DirectoryName!);

        if (Directory.Exists(source))
        {
            Directory.CreateSymbolicLink(target, source);
        }
        else
        {
            File.CreateSymbolicLink(target, source);
        }

        return $"linked {target} → {source}";
    }

    private Error Taken(string what) =>
        new("seed.taken", $"{target} is already {what}; move it away and run seed again.");

    private static bool Same(string left, string right) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(left))
            == Path.TrimEndingDirectorySeparator(Path.GetFullPath(right));
}
