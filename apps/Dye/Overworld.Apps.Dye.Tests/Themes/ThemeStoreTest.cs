using Overworld.Apps.Dye.Themes;

namespace Overworld.Apps.Dye.Tests.Themes;

public sealed class ThemeStoreTest : IDisposable
{
    private const string Dark = """
        {
          "colors": { "background": "#1C1B21", "accent": "#8E62D1", "scrim": "#9E0B0A0E" },
          "font": { "mono": ["JetBrains Mono"], "sizes": { "body": 13 } },
          "radii": { "small": 4 },
          "space": { "1": 4 }
        }
        """;

    private const string Light = """
        {
          "colors": { "background": "#F3F2F6", "accent": "#7B52C4", "scrim": "#66231F2B" },
          "font": { "mono": ["JetBrains Mono"], "sizes": { "body": 13 } },
          "radii": { "small": 4 },
          "space": { "1": 4 }
        }
        """;

    private readonly TemporaryFolder _root = new();

    public ThemeStoreTest()
    {
        Directory.CreateDirectory(System.IO.Path.Combine(Repository, "amethyst"));
        File.WriteAllText(System.IO.Path.Combine(Repository, "amethyst", "dark.json"), Dark);
        File.WriteAllText(System.IO.Path.Combine(Repository, "amethyst", "light.json"), Light);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Themes)!);
        Directory.CreateSymbolicLink(Themes, Repository);
    }

    private string Repository => System.IO.Path.Combine(_root.Path, "repo", "themes");

    private string Themes => System.IO.Path.Combine(_root.Path, "config", "themes");

    private string Active => System.IO.Path.Combine(_root.Path, "config", "theme.json");

    private ThemeStore Store => new(Themes, Active);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Dictionary<Variant, IReadOnlyDictionary<string, string>> Unchanged => [];

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task ListsThemesWithTheColorsOfBothVariants()
    {
        var themes = (await Store.ListAsync(Token)).Value;

        var amethyst = Assert.Single(themes);
        Assert.Equal("amethyst", amethyst.Name);
        Assert.False(amethyst.Active);
        Assert.Null(amethyst.Worn);
        Assert.Equal("#9E0B0A0E", amethyst.Dark["scrim"]);
        Assert.Equal("#66231F2B", amethyst.Light["scrim"]);
        Assert.Equal("#7B52C4", amethyst.Colors(Variant.Light)["accent"]);
    }

    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task RefusesAThemeWithoutBothVariants(string variant)
    {
        File.Delete(System.IO.Path.Combine(Repository, "amethyst", $"{variant}.json"));

        var found = await Store.FindAsync("amethyst", Token);

        Assert.Equal("theme.variant_missing", found.Error.Code);
        Assert.Contains(variant, found.Error.Message);
        Assert.Equal("theme.variant_missing", (await Store.ListAsync(Token)).Error.Code);
    }

    [Fact]
    public async Task SavesAThemeFromAnotherWithSomeColorsChangedInEachVariant()
    {
        var saved = await Store.SaveAsync(
            "deep-sea",
            "amethyst",
            new Dictionary<Variant, IReadOnlyDictionary<string, string>>
            {
                [Variant.Dark] = new Dictionary<string, string> { ["accent"] = "#3A7BD5" },
                [Variant.Light] = new Dictionary<string, string> { ["accent"] = "#2F62B0" },
            },
            Token);

        Assert.Equal("#3A7BD5", saved.Value.Dark["accent"]);
        Assert.Equal("#1C1B21", saved.Value.Dark["background"]);
        Assert.Equal("#2F62B0", saved.Value.Light["accent"]);
        Assert.Equal("#F3F2F6", saved.Value.Light["background"]);

        var written = await File.ReadAllTextAsync(System.IO.Path.Combine(Repository, "deep-sea", "light.json"), Token);
        Assert.Contains("\"radii\"", written);
        Assert.Contains("JetBrains Mono", written);
    }

    [Fact]
    public async Task RefusesToSaveOverATheme()
    {
        var saved = await Store.SaveAsync("amethyst", "amethyst", Unchanged, Token);

        Assert.Equal("theme.exists", saved.Error.Code);
    }

    [Fact]
    public async Task RefusesAColorTheBaseDoesNotHaveAndWritesNothing()
    {
        var saved = await Store.SaveAsync(
            "ocean",
            "amethyst",
            new Dictionary<Variant, IReadOnlyDictionary<string, string>>
            {
                [Variant.Light] = new Dictionary<string, string> { ["sparkle"] = "#FFFFFF" },
            },
            Token);

        Assert.Equal("theme.unknown_role", saved.Error.Code);
        Assert.Contains("background, accent, scrim", saved.Error.Message);
        Assert.False(Directory.Exists(System.IO.Path.Combine(Repository, "ocean")));
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("Ocean")]
    [InlineData("deep sea")]
    [InlineData("")]
    public async Task RefusesNamesThatAreNotPlainWords(string name)
    {
        var saved = await Store.SaveAsync(name, "amethyst", Unchanged, Token);
        var applied = await Store.ApplyAsync(name, Variant.Dark, Token);

        Assert.Equal("theme.name_invalid", saved.Error.Code);
        Assert.Equal("theme.name_invalid", applied.Error.Code);
    }

    [Fact]
    public async Task TellsWhenAThemeIsMissing()
    {
        Assert.Equal("theme.not_found", (await Store.FindAsync("ocean", Token)).Error.Code);
        Assert.Equal("theme.not_found", (await Store.SaveAsync("ocean", "coral", Unchanged, Token)).Error.Code);
    }

    [Fact]
    public async Task TellsWhenTheThemesAreNotLinked()
    {
        var store = new ThemeStore(System.IO.Path.Combine(_root.Path, "nowhere"), Active);

        Assert.Equal("theme.folder_missing", (await store.ListAsync(Token)).Error.Code);
    }

    [Fact]
    public async Task AppliesAVariantByLinkingItsRealFile()
    {
        await Store.SaveAsync("deep-sea", "amethyst", Unchanged, Token);

        var applied = await Store.ApplyAsync("amethyst", Variant.Dark, Token);
        var reapplied = await Store.ApplyAsync("deep-sea", Variant.Light, Token);

        Assert.Equal(Variant.Dark, applied.Value.Worn);
        Assert.Equal(Variant.Light, reapplied.Value.Worn);
        Assert.Equal(System.IO.Path.Combine(Repository, "deep-sea", "light.json"), new FileInfo(Active).LinkTarget);
        Assert.Equal(["amethyst", "deep-sea"], (await Store.ListAsync(Token)).Value.Select(theme => theme.Name));
        Assert.Equal([false, true], (await Store.ListAsync(Token)).Value.Select(theme => theme.Active));
        Assert.Single(Directory.EnumerateFileSystemEntries(System.IO.Path.GetDirectoryName(Active)!, "theme.json*"));
    }

    [Fact]
    public async Task KeepsTheVariantWornWhenOnlyTheThemeChanges()
    {
        await Store.SaveAsync("deep-sea", "amethyst", Unchanged, Token);
        await Store.ApplyAsync("amethyst", Variant.Light, Token);

        var applied = await Store.ApplyAsync("deep-sea", null, Token);

        Assert.Equal(Variant.Light, applied.Value.Worn);
    }

    [Fact]
    public async Task KeepsTheThemeWornWhenOnlyTheVariantChanges()
    {
        await Store.ApplyAsync("amethyst", Variant.Dark, Token);

        var applied = await Store.ApplyAsync(null, Variant.Light, Token);

        Assert.Equal("amethyst", applied.Value.Name);
        Assert.Equal(Variant.Light, applied.Value.Worn);
    }

    [Fact]
    public async Task WearsDarkWhenNothingIsWornAndNoVariantIsNamed()
    {
        var applied = await Store.ApplyAsync("amethyst", null, Token);

        Assert.Equal(Variant.Dark, applied.Value.Worn);
    }

    [Fact]
    public async Task AsksForAThemeWhenNoneIsWorn()
    {
        var applied = await Store.ApplyAsync(null, Variant.Light, Token);

        Assert.Equal("theme.none_worn", applied.Error.Code);
        Assert.False(File.Exists(Active));
    }

    [Fact]
    public async Task RefusesToReplaceAThemeFileThatIsNotALink()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Active)!);
        await File.WriteAllTextAsync(Active, Dark, Token);

        var applied = await Store.ApplyAsync("amethyst", Variant.Dark, Token);

        Assert.Equal("theme.taken", applied.Error.Code);
        Assert.Null(new FileInfo(Active).LinkTarget);
    }

    [Fact]
    public async Task RefusesAFileThatIsNotATheme()
    {
        Directory.CreateDirectory(System.IO.Path.Combine(Repository, "broken"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(Repository, "broken", "dark.json"), "{\"colors\": [1]}", Token);
        await File.WriteAllTextAsync(System.IO.Path.Combine(Repository, "broken", "light.json"), Light, Token);

        Assert.Equal("theme.invalid", (await Store.FindAsync("broken", Token)).Error.Code);
    }
}
