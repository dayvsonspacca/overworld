namespace Overworld.Seed.Tests;

public sealed class TemporaryFolder : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("seed-").FullName;

    public string PathTo(params string[] parts) => Path.Combine([Root, .. parts]);

    public string Folder(params string[] parts) => Directory.CreateDirectory(PathTo(parts)).FullName;

    public string File(string content, params string[] parts)
    {
        var path = PathTo(parts);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, content);

        return path;
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
