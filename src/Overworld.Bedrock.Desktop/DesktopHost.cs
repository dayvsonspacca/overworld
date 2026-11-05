using Avalonia;
using Avalonia.Controls;
using Overworld.Bedrock.Apps;

namespace Overworld.Bedrock.Desktop;

public static class DesktopHost
{
    public static int Run(AppIdentity app, Func<Control> content, string[] args) =>
        AppBuilder
            .Configure(() => new BedrockApplication(() => new Window
            {
                Title = app.Command,
                Width = 960,
                Height = 640,
                Content = content(),
            }))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
}
