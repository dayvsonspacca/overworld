using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class SettingsStoreTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string SettingsFile => _folder.PathTo(".crafter", "settings.json");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task HasNothingForAProjectWithoutSettings()
    {
        var settings = await new SettingsStore().ReadAsync(_folder.Path, Token);

        Assert.Null(settings.Value);
    }

    [Fact]
    public async Task WritesSettingsIntoTheProjectsOwnFolder()
    {
        var store = new SettingsStore();

        await store.UpdateAsync(_folder.Path, _ => Result.Success<ProjectSettings?>(new ProjectSettings("Overworld")), Token);

        Assert.True(File.Exists(SettingsFile));
        Assert.Equal(new ProjectSettings("Overworld"), (await new SettingsStore().ReadAsync(_folder.Path, Token)).Value);
    }

    [Fact]
    public async Task ReadsWhatCrafterWrote()
    {
        Directory.CreateDirectory(_folder.PathTo(".crafter"));
        await File.WriteAllTextAsync(
            SettingsFile,
            """
            {
              "name": "Overworld",
              "icon": ".crafter/icon.png",
              "axioms": [
                "Name every app's CLI command overworld:<app>, like overworld:dye list."
              ]
            }
            """,
            Token);

        var settings = (await new SettingsStore().ReadAsync(_folder.Path, Token)).Value!;

        Assert.Equal("Overworld", settings.Name);
        Assert.Equal(".crafter/icon.png", settings.Icon);
        Assert.Equal(["Name every app's CLI command overworld:<app>, like overworld:dye list."], settings.Axioms);
    }

    [Fact]
    public async Task WritesTheShapeCrafterWrote()
    {
        await new SettingsStore().UpdateAsync(
            _folder.Path,
            _ => Result.Success<ProjectSettings?>(new ProjectSettings("Overworld", Axioms: ["Keep overworld:<app> as typed."])),
            Token);

        var json = await File.ReadAllTextAsync(SettingsFile, Token);

        Assert.Contains("\"name\": \"Overworld\"", json);
        Assert.Contains("\"Keep overworld:<app> as typed.\"", json);
        Assert.DoesNotContain("icon", json);
    }

    [Fact]
    public async Task ChangesWhatIsWrittenAndKeepsTheRest()
    {
        var store = new SettingsStore();
        await store.UpdateAsync(_folder.Path, _ => Result.Success<ProjectSettings?>(new ProjectSettings("Overworld", Axioms: ["Rule"])), Token);

        await store.UpdateAsync(_folder.Path, settings => Result.Success<ProjectSettings?>(settings! with { Name = "World" }), Token);

        var settings = (await store.ReadAsync(_folder.Path, Token)).Value!;
        Assert.Equal("World", settings.Name);
        Assert.Equal(["Rule"], settings.Axioms);
    }

    [Fact]
    public async Task ReportsSettingsWithoutAName()
    {
        Directory.CreateDirectory(_folder.PathTo(".crafter"));
        await File.WriteAllTextAsync(SettingsFile, """{ "icon": "icon.png" }""", Token);

        var settings = await new SettingsStore().ReadAsync(_folder.Path, Token);

        Assert.Equal("file.unreadable", settings.Error.Code);
    }
}
