using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Overworld.Apps.Painting.Views;

public sealed class ArtworkCanvas : Decorator
{
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(250);

    public static readonly StyledProperty<string?> HtmlProperty =
        AvaloniaProperty.Register<ArtworkCanvas, string?>(nameof(Html));

    private readonly NativeWebView _web = new();

    public ArtworkCanvas()
    {
        _web.EnvironmentRequested += (_, e) =>
        {
            if (e is GtkWebViewEnvironmentRequestedEventArgs gtk)
            {
                gtk.ExperimentalOffscreen = true;
            }
        };
        _web.AdapterCreated += (_, _) => Paint();

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
