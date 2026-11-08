using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Overworld.Apps.Painting.Views;

// The page of a painting, drawn by WebKitGTK: what Painting's window shows, and what any app
// shows when it frames a painting inside itself.
public sealed class ArtworkCanvas : Decorator
{
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(250);

    public static readonly StyledProperty<string?> HtmlProperty =
        AvaloniaProperty.Register<ArtworkCanvas, string?>(nameof(Html));

    private readonly NativeWebView _web = new();

    public ArtworkCanvas()
    {
        // WebKitGTK embedded as a window needs X11; drawn offscreen, it runs in the Wayland window every app has.
        _web.EnvironmentRequested += (_, e) =>
        {
            if (e is GtkWebViewEnvironmentRequestedEventArgs gtk)
            {
                gtk.ExperimentalOffscreen = true;
            }
        };
        _web.AdapterCreated += (_, _) => Paint();

        // Drawn offscreen, a page that stops changing leaves its first frame unshown until something
        // redraws the view, so it is redrawn once the page has had time to paint.
        _web.NavigationCompleted += async (_, _) =>
        {
            await Task.Delay(Settle);
            _web.InvalidateVisual();
        };

        Child = _web;
    }

    public string? Html
    {
        get => GetValue(HtmlProperty);
        set => SetValue(HtmlProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HtmlProperty)
        {
            Paint();
        }
    }

    private void Paint()
    {
        if (Html is { } html)
        {
            _web.NavigateToString(html);
        }
    }
}
