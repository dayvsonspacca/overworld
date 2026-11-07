using Overworld.Apps.Dye.Themes;

namespace Overworld.Apps.Dye.Tests.Themes;

public sealed class GtkSheetTest : IDisposable
{
    private static readonly Dictionary<string, string> Colors = new()
    {
        ["background"] = "#1C1B21",
        ["raised"] = "#25242B",
        ["surface"] = "#2E2D35",
        ["divider"] = "#38373E",
        ["text"] = "#E4E2DC",
        ["accent"] = "#8E62D1",
        ["accentText"] = "#AB83E1",
        ["onAccent"] = "#FFFFFF",
        ["danger"] = "#E5806F",
        ["added"] = "#8CC08C",
    };

    private readonly TemporaryFolder _root = new();

    private string Sheet => System.IO.Path.Combine(_root.Path, "gtk-4.0", "gtk.css");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task WritesTheThemeAsLibadwaitaColors()
    {
        var written = await new GtkSheet(Sheet).WriteAsync(new Theme("amethyst", true, Colors), Token);

        Assert.Equal(Sheet, written.Value);
        var css = await File.ReadAllTextAsync(Sheet, Token);
        Assert.Contains("--window-bg-color: #1C1B21;", css);
        Assert.Contains("--sidebar-bg-color: #25242B;", css);
        Assert.Contains("--accent-fg-color: #FFFFFF;", css);
        Assert.Contains("--accent-color: #AB83E1;", css);
    }

    [Fact]
    public async Task RewritesItsOwnSheet()
    {
        var gtk = new GtkSheet(Sheet);
        await gtk.WriteAsync(new Theme("amethyst", true, Colors), Token);

        var written = await gtk.WriteAsync(new Theme("warden", true, new Dictionary<string, string>(Colors) { ["accent"] = "#0FA3A8" }), Token);

        Assert.True(written.IsSuccess);
        Assert.Contains("--accent-bg-color: #0FA3A8;", await File.ReadAllTextAsync(Sheet, Token));
    }

    [Fact]
    public async Task MovesAlphaToTheEndAsCssReadsIt()
    {
        await new GtkSheet(Sheet).WriteAsync(new Theme("amethyst", true, new Dictionary<string, string>(Colors) { ["divider"] = "#12FFFFFF" }), Token);

        Assert.Contains("--headerbar-border-color: #FFFFFF12;", await File.ReadAllTextAsync(Sheet, Token));
    }

    [Fact]
    public async Task LeavesASheetThatIsNotOverworldsAlone()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Sheet)!);
        await File.WriteAllTextAsync(Sheet, "window { color: red; }", Token);

        var written = await new GtkSheet(Sheet).WriteAsync(new Theme("amethyst", true, Colors), Token);

        Assert.Equal("gtk.taken", written.Error.Code);
        Assert.Equal("window { color: red; }", await File.ReadAllTextAsync(Sheet, Token));
    }

    [Fact]
    public async Task NamesTheColorsAThemeLacks()
    {
        var colors = new Dictionary<string, string>(Colors);
        colors.Remove("raised");

        var written = await new GtkSheet(Sheet).WriteAsync(new Theme("bare", true, colors), Token);

        Assert.Equal("gtk.missing_roles", written.Error.Code);
        Assert.Contains("raised", written.Error.Message);
        Assert.False(File.Exists(Sheet));
    }
}
