using Overworld.Apps.Crafter.Repositories;

namespace Overworld.Apps.Crafter.Folders;

public static class ProjectFiles
{
    public const int Most = 20_000;

    private static readonly HashSet<string> Skipped = new(StringComparer.Ordinal) { "node_modules", "vendor", "bin", "obj", "dist", "target" };

    private static readonly EnumerationOptions Visible = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
    };

    public static async Task<IReadOnlyList<string>> OfAsync(string folder, CancellationToken cancellationToken)
    {
        var files = (FolderListing.IsRepository(folder) ? await TrackedAsync(folder, cancellationToken) : null)
            ?? await Task.Run(() => Walked(folder, cancellationToken), cancellationToken);

        return [.. WithFolders(files).Order(StringComparer.Ordinal)];
    }

    private static IEnumerable<string> WithFolders(IReadOnlyList<string> files)
    {
        var folders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            yield return file;

            for (var slash = file.IndexOf('/'); slash > 0; slash = file.IndexOf('/', slash + 1))
            {
                var parent = file[..(slash + 1)];

                if (folders.Add(parent))
                {
                    yield return parent;
                }
            }
        }
    }

    private static async Task<List<string>?> TrackedAsync(string folder, CancellationToken cancellationToken)
    {
        var listed = await Git.RunAsync(["-C", folder, "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return listed.IsSuccess
            ? [.. listed.Value.Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).Take(Most)]
            : null;
    }

    private static List<string> Walked(string folder, CancellationToken cancellationToken)
    {
        List<string> files = [];
        var pending = new Queue<string>();
        pending.Enqueue(folder);

        while (files.Count < Most && pending.TryDequeue(out var next))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                foreach (var entry in new DirectoryInfo(next).EnumerateFileSystemInfos("*", Visible))
                {
                    if (entry.Name.StartsWith('.'))
                    {
                        continue;
                    }

                    if (entry is DirectoryInfo child)
                    {
                        if (!Skipped.Contains(child.Name))
                        {
                            pending.Enqueue(child.FullName);
                        }
                    }
                    else if (files.Count < Most)
                    {
                        files.Add(Path.GetRelativePath(folder, entry.FullName).Replace(Path.DirectorySeparatorChar, '/'));
                    }
                }
            }
            catch (Exception unreachable) when (unreachable is DirectoryNotFoundException or UnauthorizedAccessException or IOException)
            {
            }
        }

        return files;
    }
}
