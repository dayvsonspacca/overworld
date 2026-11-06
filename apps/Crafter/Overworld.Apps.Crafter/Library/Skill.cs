namespace Overworld.Apps.Crafter.Library;

public sealed record Skill(string Id, string Name, string Description, string? WhenToUse, string Folder)
{
    public string Instructions => Path.Combine(Folder, "instructions.md");
}
