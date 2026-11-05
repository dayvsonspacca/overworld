namespace Overworld.Seed.Tests;

public sealed class RepositoryTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void FindsTheRootFromAFolderInsideIt()
    {
        _folder.File("<Solution />", "Overworld.slnx");

        var repository = Repository.Find(_folder.Folder("apps", "Dye"));

        Assert.Equal(_folder.PathTo("config", "hypr"), repository.Value.PathTo("config", "hypr"));
    }

    [Fact]
    public void FindsNothingOutsideARepository()
    {
        var repository = Repository.Find(_folder.Folder("somewhere"));

        Assert.Equal("seed.no_repository", repository.Error.Code);
    }
}
