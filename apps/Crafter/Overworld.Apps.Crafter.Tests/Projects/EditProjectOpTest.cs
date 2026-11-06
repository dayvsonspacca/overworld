using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class EditProjectOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly SettingsStore _settings = new();
    private readonly string _overworld;

    public EditProjectOpTest()
    {
        _shelf = new Shelf(_folder.PathTo("data"));
        _overworld = Directory.CreateDirectory(_folder.PathTo("overworld")).FullName;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string Crafter(params string[] parts) => Path.Combine([_overworld, ".crafter", .. parts]);

    public void Dispose() => _folder.Dispose();

    private async Task OpenAsync(ProjectSettings? settings = null)
    {
        await _shelf.AddAsync(_overworld, Token);

        if (settings is not null)
        {
            await _settings.UpdateAsync(_overworld, _ => Result.Success<ProjectSettings?>(settings), Token);
        }
    }

    private string Image(string name, string content = "png")
    {
        var path = _folder.PathTo(name);
        File.WriteAllText(path, content);

        return path;
    }

    private Task<Result<Project>> EditAsync(EditProject input) =>
        new EditProjectOp(_shelf, _settings).ExecuteAsync(input, Token);

    private async Task<ProjectSettings?> SavedAsync() => (await new SettingsStore().ReadAsync(_overworld, Token)).Value;

    [Fact]
    public async Task RenamesAProjectAndKeepsItsAxioms()
    {
        await OpenAsync(new ProjectSettings("Old", Axioms: ["Rule"]));

        var edited = await EditAsync(new EditProject(_overworld, Name: " Overworld "));

        Assert.Equal(new Project(_overworld, "Overworld"), edited.Value);
        Assert.Equal("Overworld", (await SavedAsync())!.Name);
        Assert.Equal(["Rule"], (await SavedAsync())!.Axioms);
    }

    [Fact]
    public async Task GivesAProjectAnIconCopiedIntoItsFolder()
    {
        await OpenAsync();

        var edited = await EditAsync(new EditProject(_overworld, Icon: Image("Logo.PNG", "logo")));

        Assert.Equal(new Project(_overworld, "overworld", ".crafter/icon.png"), edited.Value);
        Assert.Equal("logo", await File.ReadAllTextAsync(Crafter("icon.png"), Token));
        Assert.Equal(new ProjectSettings("overworld", ".crafter/icon.png"), await SavedAsync());
    }

    [Fact]
    public async Task ReplacesTheIconItHadBefore()
    {
        await OpenAsync();
        await EditAsync(new EditProject(_overworld, Icon: Image("old.svg")));

        await EditAsync(new EditProject(_overworld, Icon: Image("new.png")));

        Assert.False(File.Exists(Crafter("icon.svg")));
        Assert.True(File.Exists(Crafter("icon.png")));
        Assert.Equal(".crafter/icon.png", (await SavedAsync())!.Icon);
    }

    [Fact]
    public async Task KeepsTheIconWhenGivenTheOneItAlreadyHas()
    {
        await OpenAsync();
        await EditAsync(new EditProject(_overworld, Icon: Image("logo.png", "logo")));

        var edited = await EditAsync(new EditProject(_overworld, Icon: Crafter("icon.png")));

        Assert.Equal(".crafter/icon.png", edited.Value.Icon);
        Assert.Equal("logo", await File.ReadAllTextAsync(Crafter("icon.png"), Token));
    }

    [Fact]
    public async Task TakesTheIconAway()
    {
        await OpenAsync();
        await EditAsync(new EditProject(_overworld, Icon: Image("logo.png")));

        var edited = await EditAsync(new EditProject(_overworld, RemoveIcon: true));

        Assert.Null(edited.Value.Icon);
        Assert.False(File.Exists(Crafter("icon.png")));
        Assert.Equal(new ProjectSettings("overworld"), await SavedAsync());
    }

    [Fact]
    public async Task WritesNothingWhenNothingChanges()
    {
        await OpenAsync();

        var edited = await EditAsync(new EditProject(_overworld, Name: "overworld"));

        Assert.Equal(new Project(_overworld, "overworld"), edited.Value);
        Assert.False(Directory.Exists(Crafter()));
    }

    [Fact]
    public async Task RefusesAProjectThatIsNotOpen()
    {
        var edited = await EditAsync(new EditProject(_overworld, Name: "Overworld"));

        Assert.Equal("project.not_open", edited.Error.Code);
    }

    [Fact]
    public async Task RefusesABlankName()
    {
        await OpenAsync();

        var edited = await EditAsync(new EditProject(_overworld, Name: " "));

        Assert.Equal("project.name_empty", edited.Error.Code);
    }

    [Fact]
    public async Task RefusesAnIconAndItsRemovalTogether()
    {
        await OpenAsync();

        var edited = await EditAsync(new EditProject(_overworld, Icon: Image("logo.png"), RemoveIcon: true));

        Assert.Equal("project.icon_conflict", edited.Error.Code);
    }

    [Fact]
    public async Task RefusesAnImageThatDoesNotExist()
    {
        await OpenAsync();

        var edited = await EditAsync(new EditProject(_overworld, Icon: _folder.PathTo("nowhere.png")));

        Assert.Equal("project.icon_missing", edited.Error.Code);
    }

    [Fact]
    public async Task WillNotEditOverSettingsItCannotRead()
    {
        await OpenAsync();
        Directory.CreateDirectory(Crafter());
        await File.WriteAllTextAsync(Crafter("settings.json"), "not json", Token);

        var edited = await EditAsync(new EditProject(_overworld, Icon: Image("logo.png")));

        Assert.Equal("file.unreadable", edited.Error.Code);
        Assert.False(File.Exists(Crafter("icon.png")));
    }
}
