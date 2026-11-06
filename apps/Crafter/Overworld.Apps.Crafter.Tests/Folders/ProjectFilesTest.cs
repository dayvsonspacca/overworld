using Overworld.Apps.Crafter.Folders;
using Overworld.Apps.Crafter.Tests.Library;

namespace Overworld.Apps.Crafter.Tests.Folders;

public sealed class ProjectFilesTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private void Write(string relative)
    {
        var file = _folder.PathTo(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, relative);
    }

    [Fact]
    public async Task ListsTheFilesAndTheirFoldersRelativeToTheProject()
    {
        Write("README.md");
        Write("src/Core/Prompt.cs");

        Assert.Equal(["README.md", "src/", "src/Core/", "src/Core/Prompt.cs"], await ProjectFiles.OfAsync(_folder.Path, Token));
    }

    [Fact]
    public async Task SkipsHiddenAndBuildFolders()
    {
        Write("app.cs");
        Write(".cache/state.json");
        Write("node_modules/left-pad/index.js");
        Write("bin/Debug/app.dll");

        Assert.Equal(["app.cs"], await ProjectFiles.OfAsync(_folder.Path, Token));
    }

    [Fact]
    public async Task ListsWhatGitTracksOrWouldInARepository()
    {
        Remote.Git(_folder.Path, "init", "--quiet");
        Write(".gitignore");
        File.WriteAllText(_folder.PathTo(".gitignore"), "secret.txt\n");
        Write("secret.txt");
        Write("src/App.cs");

        Assert.Equal([".gitignore", "src/", "src/App.cs"], await ProjectFiles.OfAsync(_folder.Path, Token));
    }

    [Fact]
    public async Task ListsNothingInAFolderThatDoesNotExist()
    {
        Assert.Empty(await ProjectFiles.OfAsync("/nonexistent/crafter-folder", Token));
    }
}
