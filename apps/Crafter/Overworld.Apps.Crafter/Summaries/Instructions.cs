namespace Overworld.Apps.Crafter.Summaries;

public sealed record Instructions(string File, string Text)
{
    public const string Shared = "AGENTS.md";

    public static Instructions? In(string folder, IReadOnlyList<string> files) =>
        files.Prepend(Shared).Distinct().Select(file => Read(Path.Combine(folder, file)) is { } text ? new Instructions(file, text) : null).FirstOrDefault(found => found is not null);

    private static string? Read(string file)
    {
        try
        {
            return System.IO.File.Exists(file) && System.IO.File.ReadAllText(file).Trim() is { Length: > 0 } text ? text : null;
        }
        catch (Exception unreachable) when (unreachable is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
