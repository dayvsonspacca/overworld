using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Overworld.Beacon;
using Overworld.Desktop.Theming;
using Overworld.Redstone;

namespace Overworld.Desktop;

public sealed partial class OverworldApplication(Func<Window> mainWindow) : Application
{
    public static readonly ThemeVariant Dark = new("OverworldDark", ThemeVariant.Dark);

    public static readonly ThemeVariant Light = new("OverworldLight", ThemeVariant.Light);

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        if (Resources.MergedDictionaries.OfType<ThemeResources>().FirstOrDefault() is { } theme)
        {
            Wear(theme);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow = mainWindow();
        }

        if (Resources.MergedDictionaries.OfType<ThemeResources>().FirstOrDefault() is { } theme)
        {
            _ = WearAsync(theme);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Wear(ThemeResources theme) => RequestedThemeVariant = theme.IsLight ? Light : Dark;

    private async Task WearAsync(ThemeResources theme)
    {
        var beacon = new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName));

        await foreach (var signal in beacon.WatchAsync(CancellationToken.None))
        {
            if (signal == "theme")
            {
                theme.Reload();
                Wear(theme);
            }
        }
    }
}
