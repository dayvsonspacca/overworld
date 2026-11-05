namespace Overworld.Apps.Dye.Tests;

public sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("dye-").FullName;

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
