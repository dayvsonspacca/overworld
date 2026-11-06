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
    public static readonly ThemeVariant Variant = new("Overworld", ThemeVariant.Dark);

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

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

    private static async Task WearAsync(ThemeResources theme)
    {
        var beacon = new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName));

        await foreach (var signal in beacon.WatchAsync(CancellationToken.None))
        {
            if (signal == "theme")
            {
                theme.Reload();
            }
        }
    }
}
