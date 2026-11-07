using Overworld.Seed.Steps;

namespace Overworld.Seed.Tests.Steps;

public sealed class ThemeStepTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    public ThemeStepTest()
    {
        _folder.File("{}", "themes", "amethyst", "dark.json");
        _folder.File("{}", "themes", "amethyst", "light.json");
        _folder.File("{}", "themes", "redstone", "dark.json");
        _folder.File("{}", "themes", "redstone", "light.json");
    }

    public void Dispose() => _folder.Dispose();

    private string Target => _folder.PathTo("config", "overworld", "theme.json");

    private Task<Bedrock.Results.Result<string>> Install(string? theme) =>
        new ThemeStep(_folder.PathTo("themes"), theme, Target).RunAsync(TestContext.Current.CancellationToken);

    private void Wear(params string[] path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Target)!);
        File.CreateSymbolicLink(Target, _folder.PathTo(path));
    }

    [Fact]
    public async Task LinksTheChosenThemeDark()
    {
        await Install("amethyst");

        Assert.Equal(_folder.PathTo("themes", "amethyst", "dark.json"), new FileInfo(Target).LinkTarget);
    }

    [Fact]
    public async Task SwitchesFromOneThemeToAnother()
    {
        await Install("amethyst");

        var switched = await Install("redstone");

        Assert.True(switched.IsSuccess);
        Assert.Equal(_folder.PathTo("themes", "redstone", "dark.json"), new FileInfo(Target).LinkTarget);
    }

    [Fact]
    public async Task KeepsTheVariantWornWhenSwitchingTheme()
    {
        Wear("themes", "amethyst", "light.json");

        await Install("redstone");

        Assert.Equal(_folder.PathTo("themes", "redstone", "light.json"), new FileInfo(Target).LinkTarget);
    }

    [Fact]
    public async Task LinksAmethystWhenNoneIsChosenOnANewMachine()
    {
        await Install(null);

        Assert.Equal(_folder.PathTo("themes", "amethyst", "dark.json"), new FileInfo(Target).LinkTarget);
    }

    [Fact]
    public async Task KeepsTheThemeWornWhenNoneIsChosen()
    {
        Wear("themes", "redstone", "light.json");

        var kept = await Install(null);

        Assert.EndsWith("keeps redstone light", kept.Value);
        Assert.Equal(_folder.PathTo("themes", "redstone", "light.json"), new FileInfo(Target).LinkTarget);
    }

    [Fact]
    public async Task MovesAThemeWornBeforeVariantsToItsDarkVariant()
    {
        Wear("themes", "redstone.json");

        await Install(null);

        Assert.Equal(_folder.PathTo("themes", "redstone", "dark.json"), new FileInfo(Target).LinkTarget);
    }

    [Fact]
    public async Task RefusesAThemeThatDoesNotExist()
    {
        var installed = await Install("dirt");

        Assert.Equal("seed.unknown_theme", installed.Error.Code);
        Assert.EndsWith("the themes are amethyst, redstone.", installed.Error.Message);
    }

    [Fact]
    public async Task LeavesAFileThatIsNotATheme()
    {
        _folder.File("mine", "config", "overworld", "theme.json");

        var installed = await Install("amethyst");

        Assert.Equal("seed.taken", installed.Error.Code);
        Assert.Equal("mine", await File.ReadAllTextAsync(Target, TestContext.Current.CancellationToken));
    }
}
