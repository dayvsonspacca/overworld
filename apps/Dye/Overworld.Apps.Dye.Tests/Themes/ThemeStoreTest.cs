using Overworld.Apps.Dye.Themes;

namespace Overworld.Apps.Dye.Tests.Themes;

public sealed class ThemeStoreTest : IDisposable
{
    private const string Amethyst = """
        {
          "colors": { "background": "#1C1B21", "accent": "#8E62D1", "scrim": "#9E0B0A0E" },
          "font": { "mono": ["JetBrains Mono"], "sizes": { "body": 13 } },
          "radii": { "small": 4 },
          "space": { "1": 4 }
        }
        """;

    private readonly TemporaryFolder _root = new();

    public ThemeStoreTest()
    {
        Directory.CreateDirectory(Repository);
        File.WriteAllText(System.IO.Path.Combine(Repository, "amethyst.json"), Amethyst);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Themes)!);
        Directory.CreateSymbolicLink(Themes, Repository);
    }

    private string Repository => System.IO.Path.Combine(_root.Path, "repo", "themes");

    private string Themes => System.IO.Path.Combine(_root.Path, "config", "themes");

    private string Active => System.IO.Path.Combine(_root.Path, "config", "theme.json");

    private ThemeStore Store => new(Themes, Active);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task ListsThemesWithTheirColors()
    {
        var themes = (await Store.ListAsync(Token)).Value;

        var amethyst = Assert.Single(themes);
        Assert.Equal("amethyst", amethyst.Name);
        Assert.False(amethyst.Active);
        Assert.Equal("#9E0B0A0E", amethyst.Colors["scrim"]);
    }

    [Fact]
    public async Task SavesAThemeFromAnotherWithSomeColorsChanged()
    {
        var saved = await Store.SaveAsync("deep-sea", "amethyst", new Dictionary<string, string> { ["accent"] = "#3A7BD5" }, Token);

        Assert.Equal("#3A7BD5", saved.Value.Colors["accent"]);
        Assert.Equal("#1C1B21", saved.Value.Colors["background"]);

        var written = await File.ReadAllTextAsync(System.IO.Path.Combine(Repository, "deep-sea.json"), Token);
        Assert.Contains("\"radii\"", written);
        Assert.Contains("JetBrains Mono", written);
    }

    [Fact]
    public async Task RefusesToSaveOverATheme()
    {
        var saved = await Store.SaveAsync("amethyst", "amethyst", new Dictionary<string, string>(), Token);

        Assert.Equal("theme.exists", saved.Error.Code);
    }

    [Fact]
    public async Task RefusesAColorTheBaseDoesNotHave()
    {
        var saved = await Store.SaveAsync("ocean", "amethyst", new Dictionary<string, string> { ["sparkle"] = "#FFFFFF" }, Token);

        Assert.Equal("theme.unknown_role", saved.Error.Code);
        Assert.Contains("background, accent, scrim", saved.Error.Message);
        Assert.False(File.Exists(System.IO.Path.Combine(Repository, "ocean.json")));
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("Ocean")]
    [InlineData("deep sea")]
    [InlineData("")]
    public async Task RefusesNamesThatAreNotPlainWords(string name)
    {
        var saved = await Store.SaveAsync(name, "amethyst", new Dictionary<string, string>(), Token);
        var applied = await Store.ApplyAsync(name, Token);

        Assert.Equal("theme.name_invalid", saved.Error.Code);
        Assert.Equal("theme.name_invalid", applied.Error.Code);
    }

    [Fact]
    public async Task TellsWhenAThemeIsMissing()
    {
        Assert.Equal("theme.not_found", (await Store.FindAsync("ocean", Token)).Error.Code);
        Assert.Equal("theme.not_found", (await Store.SaveAsync("ocean", "coral", new Dictionary<string, string>(), Token)).Error.Code);
    }

    [Fact]
    public async Task TellsWhenTheThemesAreNotLinked()
    {
        var store = new ThemeStore(System.IO.Path.Combine(_root.Path, "nowhere"), Active);

        Assert.Equal("theme.folder_missing", (await store.ListAsync(Token)).Error.Code);
    }

    [Fact]
    public async Task AppliesAThemeByLinkingItsRealFile()
    {
        await Store.SaveAsync("deep-sea", "amethyst", new Dictionary<string, string>(), Token);

        var applied = await Store.ApplyAsync("amethyst", Token);
        var reapplied = await Store.ApplyAsync("deep-sea", Token);

        Assert.True(applied.Value.Active);
        Assert.True(reapplied.Value.Active);
        Assert.Equal(System.IO.Path.Combine(Repository, "deep-sea.json"), new FileInfo(Active).LinkTarget);
        Assert.Equal(["amethyst", "deep-sea"], (await Store.ListAsync(Token)).Value.Select(theme => theme.Name));
        Assert.Equal([false, true], (await Store.ListAsync(Token)).Value.Select(theme => theme.Active));
        Assert.Single(Directory.EnumerateFileSystemEntries(System.IO.Path.GetDirectoryName(Active)!, "theme.json*"));
    }

    [Fact]
    public async Task RefusesToReplaceAThemeFileThatIsNotALink()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Active)!);
        await File.WriteAllTextAsync(Active, Amethyst, Token);

        var applied = await Store.ApplyAsync("amethyst", Token);

        Assert.Equal("theme.taken", applied.Error.Code);
        Assert.Null(new FileInfo(Active).LinkTarget);
    }

    [Fact]
    public async Task RefusesAFileThatIsNotATheme()
    {
        await File.WriteAllTextAsync(System.IO.Path.Combine(Repository, "broken.json"), "{\"colors\": [1]}", Token);

        Assert.Equal("theme.invalid", (await Store.FindAsync("broken", Token)).Error.Code);
    }
}
