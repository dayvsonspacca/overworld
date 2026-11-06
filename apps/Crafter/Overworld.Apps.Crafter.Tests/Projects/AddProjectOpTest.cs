using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class AddProjectOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly SettingsStore _settings = new();

    public AddProjectOpTest() => _shelf = new Shelf(_folder.PathTo("data"));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private string ProjectFolder(string name) => Directory.CreateDirectory(_folder.PathTo(name)).FullName;

    private Task<Result<Project>> AddAsync(string folder, string? name = null) =>
        new AddProjectOp(_shelf, _settings).ExecuteAsync(new AddProject(folder, name), Token);

    [Fact]
    public async Task AddsAFolderNamedAfterItself()
    {
        var overworld = ProjectFolder("overworld");

        var added = await AddAsync(overworld);

        Assert.Equal(new Project(overworld, "overworld"), added.Value);
        Assert.Equal([new Shelved(overworld)], (await _shelf.ListAsync(Token)).Value);
    }

    [Fact]
    public async Task LeavesTheFolderUntouchedWithoutAName()
    {
        var overworld = ProjectFolder("overworld");

        await AddAsync(overworld);

        Assert.False(Directory.Exists(Path.Combine(overworld, ".crafter")));
    }

    [Fact]
    public async Task KeepsTheNameAProjectAlreadyHas()
    {
        var overworld = ProjectFolder("overworld");
        await _settings.UpdateAsync(overworld, _ => Result.Success<ProjectSettings?>(new ProjectSettings("Overworld", ".crafter/icon.png")), Token);

        var added = await AddAsync(overworld);

        Assert.Equal(new Project(overworld, "Overworld", ".crafter/icon.png"), added.Value);
    }

    [Fact]
    public async Task NamesAProjectAndKeepsItsIconAndAxioms()
    {
        var overworld = ProjectFolder("overworld");
        await _settings.UpdateAsync(overworld, _ => Result.Success<ProjectSettings?>(new ProjectSettings("Old", ".crafter/icon.png", ["Rule"])), Token);

        var added = await AddAsync(overworld, "  Overworld  ");

        Assert.Equal(new Project(overworld, "Overworld", ".crafter/icon.png"), added.Value);
        var saved = (await new SettingsStore().ReadAsync(overworld, Token)).Value!;
        Assert.Equal("Overworld", saved.Name);
        Assert.Equal(["Rule"], saved.Axioms);
    }

    [Fact]
    public async Task TakesAFolderWrittenAnyWayAsTheSameProject()
    {
        var overworld = ProjectFolder("overworld");

        await AddAsync(overworld + "/");
        await AddAsync(overworld + "/./");

        Assert.Equal([new Shelved(overworld)], (await _shelf.ListAsync(Token)).Value);
    }

    [Fact]
    public async Task RefusesAFolderThatDoesNotExist()
    {
        var added = await AddAsync(_folder.PathTo("nowhere"));

        Assert.Equal("project.folder_missing", added.Error.Code);
        Assert.Empty((await _shelf.ListAsync(Token)).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefusesNoFolder(string folder)
    {
        var added = await AddAsync(folder);

        Assert.Equal("project.folder_missing", added.Error.Code);
    }

    [Fact]
    public async Task RefusesABlankName()
    {
        var added = await AddAsync(ProjectFolder("overworld"), "   ");

        Assert.Equal("project.name_empty", added.Error.Code);
        Assert.Empty((await _shelf.ListAsync(Token)).Value);
    }

    [Fact]
    public async Task AddsAProjectWhoseSettingsItCannotReadByItsFolder()
    {
        var overworld = ProjectFolder("overworld");
        Directory.CreateDirectory(Path.Combine(overworld, ".crafter"));
        await File.WriteAllTextAsync(Path.Combine(overworld, ".crafter", "settings.json"), "not json", Token);

        var added = await AddAsync(overworld);

        Assert.Equal(new Project(overworld, "overworld"), added.Value);
    }

    [Fact]
    public async Task WillNotNameOverSettingsItCannotRead()
    {
        var overworld = ProjectFolder("overworld");
        Directory.CreateDirectory(Path.Combine(overworld, ".crafter"));
        await File.WriteAllTextAsync(Path.Combine(overworld, ".crafter", "settings.json"), "not json", Token);

        var added = await AddAsync(overworld, "Overworld");

        Assert.Equal("file.unreadable", added.Error.Code);
        Assert.Equal("not json", await File.ReadAllTextAsync(Path.Combine(overworld, ".crafter", "settings.json"), Token));
        Assert.Empty((await _shelf.ListAsync(Token)).Value);
    }
}
