using System.Security.Cryptography;
using System.Text;
using Overworld.Allay.Briefs;

namespace Overworld.Allay.ClaudeCode.Cli;

internal sealed record SkillShelf(string Folder, IReadOnlyList<Skill> Skills)
{
    public static string FolderFor(string shelves, string project) =>
        Path.Combine(shelves, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(project)))[..16]);

    public static SkillShelf? Write(string folder, IReadOnlyList<Skill> skills)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        if (skills.Count == 0)
        {
            return null;
        }

        foreach (var skill in skills)
        {
            Copy(Path.GetDirectoryName(skill.InstructionsFile) ?? skill.InstructionsFile, Path.Combine(folder, skill.Id));
        }

        return new SkillShelf(folder, skills);
    }

    public string InstructionsOf(Skill skill) => Path.Combine(Folder, skill.Id, Path.GetFileName(skill.InstructionsFile));

    public bool Equals(SkillShelf? other) => other is not null && Folder == other.Folder && Skills.SequenceEqual(other.Skills);

    public override int GetHashCode() => HashCode.Combine(Folder, Skills.Count);

    private static void Copy(string from, string to)
    {
        Directory.CreateDirectory(to);

        if (!Directory.Exists(from))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(from, file);

            if (relative.Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith('.')))
            {
                continue;
            }

            var copy = Path.Combine(to, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy, overwrite: true);
        }
    }
}
