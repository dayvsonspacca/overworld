using Overworld.Apps.Crafter.Repositories;

namespace Overworld.Apps.Crafter.Tests.Repositories;

public sealed class BranchesTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("crafter-git-").FullName;

    [Fact]
    public void ReadsTheBranchOfAClone()
    {
        Head(".git", "ref: refs/heads/dayvsonspacca/limpeza/remover-evoluir\n");

        Assert.Equal("dayvsonspacca/limpeza/remover-evoluir", Branches.Of(_folder));
    }

    [Fact]
    public void FindsTheRepositoryAboveAProjectInsideIt()
    {
        Head(".git", "ref: refs/heads/main\n");
        var inner = Directory.CreateDirectory(Path.Combine(_folder, "src", "app")).FullName;

        Assert.Equal("main", Branches.Of(inner));
    }

    [Fact]
    public void ShowsADetachedHeadAsItsShortSha()
    {
        Head(".git", "3f2a9c41e0b7d6a5c4b3a2f1e0d9c8b7a6f5e4d3\n");

        Assert.Equal("3f2a9c4", Branches.Of(_folder));
    }

    [Fact]
    public void FollowsAWorktreeToItsOwnHead()
    {
        Head(Path.Combine("main", ".git", "worktrees", "feature"), "ref: refs/heads/feature/status-bar\n");
        var worktree = Directory.CreateDirectory(Path.Combine(_folder, "feature")).FullName;
        File.WriteAllText(Path.Combine(worktree, ".git"), "gitdir: ../main/.git/worktrees/feature\n");

        Assert.Equal("feature/status-bar", Branches.Of(worktree));
    }

    [Fact]
    public void HasNoBranchOutsideARepository()
    {
        Assert.Null(Branches.Of(_folder));
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private void Head(string gitFolder, string content)
    {
        var git = Directory.CreateDirectory(Path.Combine(_folder, gitFolder)).FullName;
        File.WriteAllText(Path.Combine(git, "HEAD"), content);
    }
}
