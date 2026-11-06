using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class MoveProjectOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly SettingsStore _settings = new();

    public MoveProjectOpTest() => _shelf = new Shelf(_folder.PathTo("data"));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private string ProjectFolder(string name) => Directory.CreateDirectory(_folder.PathTo(name)).FullName;

    private Task<Result<Project>> MoveAsync(string folder, int to) =>
        new MoveProjectOp(_shelf, _settings).ExecuteAsync(new MoveProject(folder, to), Token);

    [Fact]
    public async Task MovesTheProjectOnTheRail()
    {
        var overworld = ProjectFolder("overworld");
        var zarb = ProjectFolder("zarb");
        await _shelf.AddAsync(overworld, Token);
        await _shelf.AddAsync(zarb, Token);

        var moved = await MoveAsync(zarb + "/", 0);

        Assert.Equal(new Project(zarb, "zarb"), moved.Value);
        Assert.Equal([new Shelved(zarb), new Shelved(overworld)], (await _shelf.ListAsync(Token)).Value);
    }

    [Fact]
    public async Task RefusesAPlaceBeforeTheTop()
    {
        var overworld = ProjectFolder("overworld");
        await _shelf.AddAsync(overworld, Token);

        var moved = await MoveAsync(overworld, -1);

        Assert.Equal("project.place_invalid", moved.Error.Code);
    }

    [Fact]
    public async Task RefusesAFolderThatIsNotOpen()
    {
        var moved = await MoveAsync("/work/nowhere", 0);

        Assert.Equal("project.not_open", moved.Error.Code);
    }
}
