using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Overworld.Bedrock.Apps;
using Overworld.Desktop.Commands;
using Overworld.Desktop.Windowing;

namespace Overworld.Desktop;

public static class DesktopHost
{
    private static readonly KeyGesture Palette = new(Key.P, KeyModifiers.Control | KeyModifiers.Shift);

    private static readonly KeyGesture Shortcuts = new(Key.OemQuestion, KeyModifiers.Control);

    public static int Run(AppIdentity app, Func<Control> content, string[] args) =>
        Run(app, _ => content(), args);

    public static int Run(AppIdentity app, Func<CommandBook, Control> content, string[] args) =>
        AppBuilder
            .Configure(() => new OverworldApplication(() => Window(app, content)))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);

    internal static Window Window(AppIdentity app, Func<CommandBook, Control> content)
    {
        var icon = Icon();
        var book = new CommandBook();
        var keymap = new Keymap(book);
        var palette = new CommandPalette { Book = book };

        var window = new Window
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
                    new Panel { Children = { content(book), palette } },
                },
            },
        };

        window.AddHandler(InputElement.KeyDownEvent, (_, e) => Pressed(e, palette, keymap), RoutingStrategies.Bubble);

        return window;
    }

    private static void Pressed(KeyEventArgs e, CommandPalette palette, Keymap keymap)
    {
        if (e.Handled || palette.IsOpen)
        {
            return;
        }

        if (Palette.Matches(e) || Shortcuts.Matches(e) || (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Divide))
        {
            palette.Open(shortcutsOnly: !Palette.Matches(e));
            e.Handled = true;

            return;
        }

        e.Handled = keymap.Handle(e);
    }

    private static Bitmap? Icon()
    {
        var uri = new Uri($"avares://{Assembly.GetEntryAssembly()?.GetName().Name}/icon.png");

        return AssetLoader.Exists(uri) ? new Bitmap(AssetLoader.Open(uri)) : null;
    }
}
