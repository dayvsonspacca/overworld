namespace Overworld.Apps.Crafter.Presentation.Projects;

public sealed record RepositoryChoice(string Folder, string Place)
{
    public string Name => Path.GetFileName(Folder);
}
