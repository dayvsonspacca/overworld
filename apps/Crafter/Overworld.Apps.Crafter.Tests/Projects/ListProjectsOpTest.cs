using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class ListProjectsOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly SettingsStore _settings = new();

    public ListProjectsOpTest() => _shelf = new Shelf(_folder.PathTo("data"));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private string ProjectFolder(string name) => Directory.CreateDirectory(_folder.PathTo(name)).FullName;

    private Task<Result<IReadOnlyList<Project>>> ListAsync() =>
        new ListProjectsOp(_shelf, _settings).ExecuteAsync(new ListProjects(), Token);

    [Fact]
    public async Task ListsNothingBeforeAnyProject()
    {
        Assert.Empty((await ListAsync()).Value);
    }

    [Fact]
    public async Task ListsEachProjectWithItsSettings()
    {
        var overworld = ProjectFolder("overworld");
        var zarb = ProjectFolder("zarb");
        await _shelf.AddAsync(overworld, Token);
        await _shelf.AddAsync(zarb, Token);
        await _settings.UpdateAsync(overworld, _ => Result.Success<ProjectSettings?>(new ProjectSettings("Overworld", ".crafter/icon.png")), Token);

        var listed = await ListAsync();

        Assert.Equal([new Project(overworld, "Overworld", ".crafter/icon.png"), new Project(zarb, "zarb")], listed.Value);
    }

    [Fact]
    public async Task ListsAProjectWhoseSettingsItCannotReadByItsFolder()
    {
        var overworld = ProjectFolder("overworld");
        Directory.CreateDirectory(Path.Combine(overworld, ".crafter"));
        await File.WriteAllTextAsync(Path.Combine(overworld, ".crafter", "settings.json"), "not json", Token);
        await _shelf.AddAsync(overworld, Token);

        var listed = await ListAsync();

        Assert.Equal([new Project(overworld, "overworld")], listed.Value);
    }

    [Fact]
    public async Task ReportsAShelfItCannotRead()
    {
        Directory.CreateDirectory(_folder.PathTo("data"));
        await File.WriteAllTextAsync(_folder.PathTo("data", "projects.json"), "not json", Token);

        var listed = await ListAsync();

        Assert.Equal("file.unreadable", listed.Error.Code);
    }
}
