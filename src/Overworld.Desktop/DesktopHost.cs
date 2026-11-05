using Avalonia;
using Avalonia.Controls;
using Overworld.Bedrock.Apps;

namespace Overworld.Desktop;

public static class DesktopHost
{
    public static int Run(AppIdentity app, Func<Control> content, string[] args) =>
        AppBuilder
            .Configure(() => new OverworldApplication(() => new Window
            {
                Title = app.Command,
                Width = 960,
                Height = 640,
                Content = content(),
            }))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
}
