using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Overworld.Bedrock.Apps;
using Overworld.Desktop.Windowing;

namespace Overworld.Desktop;

public static class DesktopHost
{
    public static int Run(AppIdentity app, Func<Control> content, string[] args) =>
        AppBuilder
            .Configure(() => new OverworldApplication(() => Window(app, content())))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);

    private static Window Window(AppIdentity app, Control content)
    {
        var icon = Icon();

        return new Window
        {
            Title = app.Title,
            Icon = icon is null ? null : new WindowIcon(icon),
            Width = 960,
            Height = 640,
            WindowDecorations = WindowDecorations.None,
            Content = new DockPanel
            {
                Children =
                {
                    new TitleBar { Title = app.Title, Icon = icon, [DockPanel.DockProperty] = Dock.Top },
                    content,
                },
            },
        };
    }

    private static Bitmap? Icon()
    {
        var uri = new Uri($"avares://{Assembly.GetEntryAssembly()?.GetName().Name}/icon.png");

        return AssetLoader.Exists(uri) ? new Bitmap(AssetLoader.Open(uri)) : null;
    }
}
