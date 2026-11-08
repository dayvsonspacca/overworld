using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Overworld.Apps.Painting.Views;

public sealed partial class ArtworkCanvas : Decorator
{
    private const string Undraggable =
        "<style>*{-webkit-user-drag:none}</style>" +
        "<script>addEventListener('dragstart',e=>e.preventDefault(),true)</script>";

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
            _web.NavigateToString(Pinned(html));
        }
    }

    private static string Pinned(string html)
    {
        var doctype = Doctype().Match(html);
        var at = doctype.Success ? doctype.Index + doctype.Length : 0;
        return html.Insert(at, Undraggable);
    }

    [GeneratedRegex(@"^\s*<!doctype[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex Doctype();
}
