using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class RemoveProjectOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly SettingsStore _settings = new();

    public RemoveProjectOpTest() => _shelf = new Shelf(_folder.PathTo("data"));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private string ProjectFolder(string name) => Directory.CreateDirectory(_folder.PathTo(name)).FullName;

    private Task<Result<Project>> RemoveAsync(string folder) =>
        new RemoveProjectOp(_shelf, _settings).ExecuteAsync(new RemoveProject(folder), Token);

    [Fact]
    public async Task TakesTheProjectOffAndKeepsItsSettings()
    {
        var overworld = ProjectFolder("overworld");
        var zarb = ProjectFolder("zarb");
        await _shelf.AddAsync(overworld, Token);
        await _shelf.AddAsync(zarb, Token);
        await _settings.UpdateAsync(overworld, _ => Result.Success<ProjectSettings?>(new ProjectSettings("Overworld")), Token);

        var removed = await RemoveAsync(overworld + "/");

        Assert.Equal(new Project(overworld, "Overworld"), removed.Value);
        Assert.Equal([new Shelved(zarb)], (await _shelf.ListAsync(Token)).Value);
        Assert.True(File.Exists(Path.Combine(overworld, ".crafter", "settings.json")));
    }

    [Fact]
    public async Task RemovesAProjectWhoseFolderIsGone()
    {
        var gone = _folder.PathTo("gone");
        await _shelf.AddAsync(gone, Token);

        var removed = await RemoveAsync(gone);

        Assert.Equal(new Project(gone, "gone"), removed.Value);
        Assert.Empty((await _shelf.ListAsync(Token)).Value);
    }

    [Theory]
    [InlineData("/work/nowhere")]
    [InlineData("")]
    public async Task RefusesAFolderThatIsNotOpen(string folder)
    {
        var removed = await RemoveAsync(folder);

        Assert.Equal("project.not_open", removed.Error.Code);
    }
}
