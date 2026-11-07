using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Overworld.Desktop.Theming;
using SkiaSharp;

namespace Overworld.Desktop.Tests;

public sealed class TestApp : Application
{
    private static readonly Uri Desktop = new("avares://Overworld.Desktop/");

    private static readonly string[] Dictionaries = ["Metrics", "TextField", "SidebarSection", "Sidebar", "TitleBar", "Modal", "StatusBar", "CommandPalette"];

    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() => HeadlessUnitTestSession.StartNew(typeof(TestApp)));

    public static void Run(Action test) => Session.Value.Dispatch(test, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .With(new FontManagerOptions { DefaultFamilyName = Sans });

    private static readonly string[] Families = ["Adwaita Sans", "Noto Sans", "DejaVu Sans", "Liberation Sans", "FreeSans"];

    private static string Sans =>
        Families
            .FirstOrDefault(family => SKFontManager.Default.GetFontFamilies().Contains(family)) ?? "Adwaita Sans";

    private static string Theme
    {
        get
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);

            while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "Overworld.slnx")))
            {
                folder = folder.Parent;
            }

            return Path.Combine(folder?.FullName ?? throw new DirectoryNotFoundException("The repository holding Overworld.slnx was not found."), "config", "overworld", "themes", "amethyst", "dark.json");
        }
    }

    public override void Initialize()
    {
        _ = typeof(OverworldApplication).Assembly;
        RequestedThemeVariant = OverworldApplication.Variant;
        Resources.MergedDictionaries.Add(new ThemeResources(Theme));

        foreach (var dictionary in Dictionaries)
        {
            Resources.MergedDictionaries.Add(new ResourceInclude(Desktop) { Source = new Uri($"avares://Overworld.Desktop/Themes/{dictionary}.axaml") });
        }

        Resources.ThemeDictionaries[OverworldApplication.Variant] = new ResourceInclude(Desktop) { Source = new Uri("avares://Overworld.Desktop/Themes/Fluent.axaml") };
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(Desktop) { Source = new Uri("avares://Overworld.Desktop/Themes/Controls.axaml") });
    }
}
