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
    private const string InputMethod = "AVALONIA_IM_MODULE";

    private static readonly KeyGesture Palette = new(Key.P, KeyModifiers.Control | KeyModifiers.Shift);

    private static readonly KeyGesture Shortcuts = new(Key.OemQuestion, KeyModifiers.Control);

    public static int Run(AppIdentity app, Func<Control> content, string[] args) =>
        Run(app, _ => content(), args);

    public static int Run(AppIdentity app, Func<Frame, Control> content, string[] args)
    {
        // Avalonia's IBus integration drops dead keys, so á, é, õ and ê never arrive; without
        // it the keyboard layout composes them. An input method chosen on purpose still wins.
        if (Environment.GetEnvironmentVariable(InputMethod) is null)
        {
            Environment.SetEnvironmentVariable(InputMethod, "none");
        }

        // Through XWayland, Hyprland hands a drop from a Wayland app over as the clipboard
        // instead of the dragged files; talking Wayland directly, the drop arrives as sent.
        return AppBuilder
            .Configure(() => new OverworldApplication(() => Window(app, content)))
            .UsePlatformDetect()
            .UseWaylandWithFallback()
            .With(new WaylandPlatformOptions { AppId = app.Name })
            .StartWithClassicDesktopLifetime(args);
    }

    internal static Window Window(AppIdentity app, Func<Frame, Control> content)
    {
        var icon = Icon();
        var book = new CommandBook();
        var bar = new TitleBar { Title = app.Title, Icon = icon, [DockPanel.DockProperty] = Dock.Top };
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
                    bar,
                    new Panel { Children = { content(new Frame(book, bar)), palette } },
                },
            },
        };

        bar.PropertyChanged += (_, change) =>
        {
            if (change.Property == TitleBar.TitleProperty)
            {
                window.Title = bar.Title ?? app.Title;
            }
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
