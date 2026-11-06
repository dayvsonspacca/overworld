using Overworld.Apps.Crafter.Folders;

namespace Overworld.Apps.Crafter.Tests.Folders;

public sealed class FoldersTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private string Make(params string[] parts) => Directory.CreateDirectory(_folder.PathTo(parts)).FullName;

    private string Repository(params string[] parts)
    {
        var folder = Make(parts);
        Directory.CreateDirectory(Path.Combine(folder, ".git"));

        return folder;
    }

    [Fact]
    public void ListsVisibleChildrenInOrderWhateverTheirCase()
    {
        var zarb = Make("zarb");
        var abraz = Make("Abraz");
        Make(".cache");
        File.WriteAllText(_folder.PathTo("notes.txt"), "");

        Assert.Equal([abraz, zarb], FolderListing.Children(_folder.Path));
    }

    [Fact]
    public void ListsNothingForAFolderThatIsGone()
    {
        Assert.Empty(FolderListing.Children(_folder.PathTo("gone")));
    }

    [Fact]
    public void TellsARepositoryByItsGitFolderOrFile()
    {
        var repository = Repository("overworld");
        var worktree = Make("worktree");
        File.WriteAllText(Path.Combine(worktree, ".git"), "gitdir: ../overworld/.git/worktrees/x");

        Assert.True(FolderListing.IsRepository(repository));
        Assert.True(FolderListing.IsRepository(worktree));
        Assert.False(FolderListing.IsRepository(Make("plain")));
    }

    [Fact]
    public void FindsRepositoriesWithoutLookingInsideThem()
    {
        var overworld = Repository("me", "overworld");
        Repository("me", "overworld", "vendored");
        var zarb = Repository("work", "clients", "zarb");

        Assert.Equal([overworld, zarb], new RepositoryFinder().Find(_folder.Path, TestContext.Current.CancellationToken).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LooksNoDeeperThanItsDepthNorInsideDependencies()
    {
        var shallow = Repository("one", "shallow");
        Repository("one", "two", "three", "deep");
        Repository("node_modules", "package");

        Assert.Equal([shallow], new RepositoryFinder(depth: 3).Find(_folder.Path, TestContext.Current.CancellationToken));
    }
}
