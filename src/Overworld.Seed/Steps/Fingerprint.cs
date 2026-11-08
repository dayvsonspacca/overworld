using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Overworld.Seed.Steps;

public sealed class Fingerprint(IReadOnlyList<string> settings)
{
    private static readonly string[] Built = ["bin", "obj"];

    public string Of(string project)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var file in Files(project).Concat(settings.Where(File.Exists)).Distinct().Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(file));
            hash.AppendData(File.ReadAllBytes(file));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static IEnumerable<string> Files(string project)
    {
        var folders = new HashSet<string>();
        var waiting = new Queue<string>([Path.GetFullPath(project)]);

        while (waiting.TryDequeue(out var folder))
        {
            if (!folders.Add(folder))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*.csproj"))
            {
                foreach (var reference in XDocument.Load(file).Descendants("ProjectReference"))
                {
                    if (reference.Attribute("Include")?.Value is { } included)
                    {
                        waiting.Enqueue(Path.GetDirectoryName(Path.GetFullPath(included.Replace('\\', '/'), folder))!);
                    }
                }
            }
        }

        return folders.SelectMany(folder => Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file => !Built.Any(built => Path.GetRelativePath(folder, file).Split(Path.DirectorySeparatorChar)[0] == built)));
    }
}
