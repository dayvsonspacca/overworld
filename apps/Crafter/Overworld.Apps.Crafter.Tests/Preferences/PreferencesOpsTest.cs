using Overworld.Apps.Crafter.Preferences;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Preferences;

public sealed class PreferencesOpsTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly PreferencesStore _store;

    public PreferencesOpsTest() => _store = new PreferencesStore(_folder.PathTo("data"), "/home/someone");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string PreferencesFile => _folder.PathTo("data", "preferences.json");

    public void Dispose() => _folder.Dispose();

    private Task<Result<UserPreferences>> ShowAsync() =>
        new ShowPreferencesOp(_store).ExecuteAsync(new ShowPreferences(), Token);

    private Task<Result<UserPreferences>> SetRootAsync(string folder) =>
        new SetProjectsRootOp(_store).ExecuteAsync(new SetProjectsRoot(folder), Token);

    [Fact]
    public async Task StartsTheProjectsRootAtHome()
    {
        Assert.Equal("/home/someone", (await ShowAsync()).Value.ProjectsRoot);
        Assert.False(File.Exists(PreferencesFile));
    }

    [Fact]
    public async Task MovesTheProjectsRoot()
    {
        var work = Directory.CreateDirectory(_folder.PathTo("work")).FullName;

        var set = await SetRootAsync(work + "/");

        Assert.Equal(work, set.Value.ProjectsRoot);
        Assert.Equal(work, (await ShowAsync()).Value.ProjectsRoot);
    }

    [Fact]
    public async Task RefusesAFolderThatDoesNotExist()
    {
        Assert.Equal("preferences.folder_missing", (await SetRootAsync(_folder.PathTo("nowhere"))).Error.Code);
        Assert.Equal("preferences.folder_missing", (await SetRootAsync(" ")).Error.Code);
    }

    [Fact]
    public async Task ReadsCrafterPreferencesAndSkipsWhatOverworldDropped()
    {
        Directory.CreateDirectory(_folder.PathTo("data"));
        await File.WriteAllTextAsync(
            PreferencesFile,
            """
            {
              "projectsRoot": "/work",
              "theme": "amethyst",
              "systemDark": true
            }
            """,
            Token);

        Assert.Equal("/work", (await ShowAsync()).Value.ProjectsRoot);
    }

    [Fact]
    public async Task ReportsPreferencesItCannotRead()
    {
        Directory.CreateDirectory(_folder.PathTo("data"));
        await File.WriteAllTextAsync(PreferencesFile, "not json", Token);

        Assert.Equal("file.unreadable", (await ShowAsync()).Error.Code);
    }
}
