namespace Overworld.Apps.Painting.Tests;

public sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("painting-").FullName;

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
