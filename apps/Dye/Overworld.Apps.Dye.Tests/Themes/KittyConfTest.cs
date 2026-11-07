using Overworld.Apps.Dye.Themes;

namespace Overworld.Apps.Dye.Tests.Themes;

public sealed class KittyConfTest : IDisposable
{
    private static readonly Dictionary<string, string> Colors = new()
    {
        ["background"] = "#1C1B21",
        ["raised"] = "#25242B",
        ["border"] = "#4A4952",
        ["subtle"] = "#8A8888",
        ["text"] = "#E4E2DC",
        ["accent"] = "#8E62D1",
        ["accentText"] = "#AB83E1",
        ["selected"] = "#332944",
        ["danger"] = "#E5806F",
        ["black"] = "#2E2D35",
        ["red"] = "#E5806F",
        ["green"] = "#8CC08C",
        ["yellow"] = "#E2C07A",
        ["blue"] = "#7F9CE6",
        ["magenta"] = "#AB83E1",
        ["cyan"] = "#6FC3C4",
        ["white"] = "#B7B5B2",
        ["brightBlack"] = "#615F63",
        ["brightRed"] = "#EE9A8B",
        ["brightGreen"] = "#A6D4A3",
        ["brightYellow"] = "#EDD49A",
        ["brightBlue"] = "#9DB4F0",
        ["brightMagenta"] = "#C9A5F2",
        ["brightCyan"] = "#8FD6D5",
        ["brightWhite"] = "#E4E2DC",
    };

    private readonly TemporaryFolder _root = new();

    private string Conf => System.IO.Path.Combine(_root.Path, "kitty", "overworld.conf");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task WritesTheThemeAsKittyOptions()
    {
        var written = await new KittyConf(Conf).WriteAsync("amethyst", Colors, Token);

        Assert.Equal(Conf, written.Value);
        var conf = await File.ReadAllTextAsync(Conf, Token);
        Assert.Contains("background #1C1B21", conf);
        Assert.Contains("cursor #8E62D1", conf);
        Assert.Contains("selection_background #332944", conf);
        Assert.Contains("color0 #2E2D35", conf);
        Assert.Contains("color15 #E4E2DC", conf);
    }

    [Fact]
    public async Task RewritesItsOwnConf()
    {
        var kitty = new KittyConf(Conf);
        await kitty.WriteAsync("amethyst", Colors, Token);

        var written = await kitty.WriteAsync("warden", new Dictionary<string, string>(Colors) { ["accent"] = "#0FA3A8" }, Token);

        Assert.True(written.IsSuccess);
        Assert.Contains("cursor #0FA3A8", await File.ReadAllTextAsync(Conf, Token));
    }

    [Fact]
    public async Task DropsAlphaKittyCannotDraw()
    {
        await new KittyConf(Conf).WriteAsync("amethyst", new Dictionary<string, string>(Colors) { ["border"] = "#12FFFFFF" }, Token);

        Assert.Contains("inactive_border_color #FFFFFF", await File.ReadAllTextAsync(Conf, Token));
    }

    [Fact]
    public async Task LeavesAConfThatIsNotOverworlds()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Conf)!);
        File.WriteAllText(Conf, "background #000000");

        var written = await new KittyConf(Conf).WriteAsync("amethyst", Colors, Token);

        Assert.Equal("kitty.taken", written.Error.Code);
        Assert.Equal("background #000000", File.ReadAllText(Conf));
    }

    [Fact]
    public async Task NamesTheColorsATheme()
    {
        var colors = new Dictionary<string, string>(Colors);
        colors.Remove("brightCyan");

        var written = await new KittyConf(Conf).WriteAsync("amethyst", colors, Token);

        Assert.Equal("kitty.missing_roles", written.Error.Code);
        Assert.Contains("brightCyan", written.Error.Message);
        Assert.False(File.Exists(Conf));
    }
}
