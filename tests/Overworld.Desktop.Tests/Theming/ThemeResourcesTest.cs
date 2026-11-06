using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Overworld.Desktop.Theming;

namespace Overworld.Desktop.Tests.Theming;

public sealed class ThemeResourcesTest : IDisposable
{
    private const string Theme = """
        {
          "colors": { "accent": "#8E62D1", "textSoft": "#B7B5B2", "scrim": "#9E0B0A0E" },
          "font": { "mono": ["JetBrains Mono", "monospace"], "sizes": { "body": 13, "largeTitle": 26 } },
          "radii": { "control": 6 },
          "space": { "4": 16 }
        }
        """;

    private readonly string _path = Path.Combine(Directory.CreateTempSubdirectory("theme-").FullName, "theme.json");

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_path)!, recursive: true);

    private ThemeResources Read(string json)
    {
        File.WriteAllText(_path, json);

        return new ThemeResources(_path);
    }

    [Fact]
    public void GivesEachColorAColorAndABrush()
    {
        TestApp.Run(() =>
        {
            var theme = Read(Theme);

            Assert.Equal(Color.Parse("#B7B5B2"), theme["TextSoftColor"]);
            Assert.Equal(Color.Parse("#9E0B0A0E"), ((SolidColorBrush)theme["ScrimBrush"]!).Color);
        });
    }

    [Fact]
    public void GivesTheFontSizesRadiiAndSpace()
    {
        TestApp.Run(() =>
        {
            var theme = Read(Theme);

            Assert.Equal(13.0, theme["FontSizeBody"]);
            Assert.Equal(26.0, theme["FontSizeLargeTitle"]);
            Assert.Equal(new CornerRadius(6), theme["RadiusControl"]);
            Assert.Equal(16.0, theme["Space4"]);
            Assert.Equal(["JetBrains Mono", "monospace"], ((FontFamily)theme["MonoFont"]!).FamilyNames);
        });
    }

    [Fact]
    public void GlowsFocusInTheThemesAccent()
    {
        TestApp.Run(() =>
        {
            var glow = ((BoxShadows)Read(Theme)["FocusGlow"]!)[0];

            Assert.Equal(Color.FromArgb(0x38, 0x8E, 0x62, 0xD1), glow.Color);
            Assert.Equal(3, glow.Spread);
        });
    }

    [Fact]
    public void TellsToRunSeedWhenNoThemeIsInstalled()
    {
        TestApp.Run(() =>
        {
            var thrown = Assert.Throws<FileNotFoundException>(() => new ThemeResources(_path));

            Assert.Contains("just seed", thrown.Message);
        });
    }

    [Fact]
    public void NamesTheSectionAThemeIsMissing()
    {
        TestApp.Run(() =>
        {
            var thrown = Assert.Throws<InvalidDataException>(() => Read("""{ "colors": {} }"""));

            Assert.Contains("'font'", thrown.Message);
        });
    }

    [Fact]
    public void RepaintsItsColorsWhenReloaded()
    {
        TestApp.Run(() =>
        {
            var theme = Read(Theme);
            File.WriteAllText(_path, Theme.Replace("#8E62D1", "#3A7BD5"));

            theme.Reload();

            Assert.Equal(Color.Parse("#3A7BD5"), theme["AccentColor"]);
            Assert.Equal(Color.Parse("#3A7BD5"), ((SolidColorBrush)theme["AccentBrush"]!).Color);
            Assert.Equal(Color.FromArgb(0x38, 0x3A, 0x7B, 0xD5), ((BoxShadows)theme["FocusGlow"]!)[0].Color);
        });
    }

    [Fact]
    public void KeepsItsColorsWhenTheThemeCannotBeRead()
    {
        TestApp.Run(() =>
        {
            var theme = Read(Theme);
            File.WriteAllText(_path, "{ not a theme");

            theme.Reload();

            Assert.Equal(Color.Parse("#8E62D1"), theme["AccentColor"]);
        });
    }

    [Fact]
    public void ControlsFollowTheColorsWhenReloaded()
    {
        TestApp.Run(() =>
        {
            var theme = Application.Current!.Resources.MergedDictionaries.OfType<ThemeResources>().Single();
            var window = new Window { Content = new CheckBox { IsChecked = true } };
            window.Show();
            Text.Page.Settle();
            var accent = (Color)theme["AccentColor"]!;

            try
            {
                theme["AccentColor"] = Colors.Red;
                Text.Page.Settle();

                Assert.True(window.TryFindResource("CheckBoxCheckBackgroundFillChecked", window.ActualThemeVariant, out var brush));
                Assert.Equal(Colors.Red, ((SolidColorBrush)brush!).Color);
            }
            finally
            {
                theme["AccentColor"] = accent;
                window.Close();
            }
        });
    }
}
