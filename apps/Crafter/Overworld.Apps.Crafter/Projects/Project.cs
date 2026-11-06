namespace Overworld.Apps.Crafter.Projects;

public sealed record Project(string Folder, string Name, string? Icon = null)
{
    public string? IconFile => Icon is null ? null : Path.GetFullPath(Path.Combine(Folder, Icon));

    public static string Normalize(string folder) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));

    public static Project Of(string folder, ProjectSettings? settings) =>
        new(folder, settings?.Name ?? (Path.GetFileName(folder) is { Length: > 0 } name ? name : folder), settings?.Icon);
}
