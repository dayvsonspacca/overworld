using Overworld.Apps.Dye.Themes;

namespace Overworld.Apps.Dye.Tests.Themes;

public sealed class ThemeOpsTest : IDisposable
{
    private readonly TemporaryFolder _root = new();

    private readonly ThemeStore _store;

    public ThemeOpsTest()
    {
        var themes = System.IO.Path.Combine(_root.Path, "themes");
        Directory.CreateDirectory(themes);
        File.WriteAllText(
            System.IO.Path.Combine(themes, "amethyst.json"),
            """{ "colors": { "background": "#1C1B21", "accent": "#8E62D1", "scrim": "#9E0B0A0E" } }""");
        _store = new ThemeStore(themes, System.IO.Path.Combine(_root.Path, "theme.json"));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task ListsTheThemes()
    {
        var themes = await new ListThemesOp(_store).ExecuteAsync(new ListThemes(), Token);

        Assert.Equal(["amethyst"], themes.Value.Select(theme => theme.Name));
    }

    [Fact]
    public async Task ShowsAThemeByItsTrimmedName()
    {
        var theme = await new ShowThemeOp(_store).ExecuteAsync(new ShowTheme(" amethyst "), Token);

        Assert.Equal("#8E62D1", theme.Value.Colors["accent"]);
    }

    [Fact]
    public async Task SavesTheColorChangesInUppercase()
    {
        var saved = await new SaveThemeOp(_store).ExecuteAsync(
            new SaveTheme(" deep-sea ", "amethyst", ["accent=#3a7bd5", " scrim = #CC000000 "]),
            Token);

        Assert.Equal("deep-sea", saved.Value.Name);
        Assert.Equal("#3A7BD5", saved.Value.Colors["accent"]);
        Assert.Equal("#CC000000", saved.Value.Colors["scrim"]);
        Assert.Equal("#1C1B21", saved.Value.Colors["background"]);
    }

    [Fact]
    public async Task SavesACopyWhenNoColorChanges()
    {
        var saved = await new SaveThemeOp(_store).ExecuteAsync(new SaveTheme("copy", "amethyst"), Token);

        Assert.Equal("#8E62D1", saved.Value.Colors["accent"]);
    }

    [Theory]
    [InlineData("accent")]
    [InlineData("accent=")]
    [InlineData("=#3A7BD5")]
    [InlineData("accent=3A7BD5")]
    [InlineData("accent=#3A7BD")]
    [InlineData("accent=#GGGGGG")]
    public async Task RefusesAChangeThatIsNotRoleAndHex(string change)
    {
        var saved = await new SaveThemeOp(_store).ExecuteAsync(new SaveTheme("ocean", "amethyst", [change]), Token);

        Assert.Equal("theme.color_invalid", saved.Error.Code);
    }

    [Fact]
    public async Task RefusesTheSameRoleTwice()
    {
        var saved = await new SaveThemeOp(_store).ExecuteAsync(
            new SaveTheme("ocean", "amethyst", ["accent=#3A7BD5", "accent=#101418"]),
            Token);

        Assert.Equal("theme.color_repeated", saved.Error.Code);
    }
}
