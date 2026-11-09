using Overworld.Web.Serving;

namespace Overworld.Web.Tests.Serving;

public sealed class ThemeSheetTest : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"theme-{Guid.NewGuid():N}.json");

    public void Dispose() => File.Delete(_file);

    [Fact]
    public void TurnsTheThemeIntoCssVariables()
    {
        File.WriteAllText(_file, """
            {
              "colors": { "background": "#1C1B21", "textSoft": "#B7B5B2", "scrim": "#9E0B0A0E" },
              "font": { "mono": ["JetBrains Mono", "monospace"], "sizes": { "body": 13 } },
              "radii": { "control": 6 },
              "space": { "2": 8 }
            }
            """);

        var css = ThemeSheet.Css(_file);

        Assert.Contains("--background: #1C1B21;", css);
        Assert.Contains("--text-soft: #B7B5B2;", css);
        Assert.Contains("--scrim: #0B0A0E9e;", css);
        Assert.Contains("--mono: \"JetBrains Mono\", monospace;", css);
        Assert.Contains("--fs-body: 13px;", css);
        Assert.Contains("--r-control: 6px;", css);
        Assert.Contains("--s-2: 8px;", css);
        Assert.Contains("color-scheme: dark;", css);
    }
}
