using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform;
using Overworld.Apps.Painting.Presentation;
using Overworld.Desktop;
using Overworld.Desktop.Windowing;

namespace Overworld.Apps.Painting.Desktop;

public sealed partial class PaintingView : UserControl
{
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(250);

    public PaintingView()
    {
        InitializeComponent();

        // WebKitGTK embedded as a window needs X11; drawn offscreen, it runs in the Wayland window every app has.
        Canvas.EnvironmentRequested += (_, e) =>
        {
            if (e is GtkWebViewEnvironmentRequestedEventArgs gtk)
            {
                gtk.ExperimentalOffscreen = true;
            }
        };
        Canvas.AdapterCreated += (_, _) => Paint();

        // Drawn offscreen, a page that stops changing leaves its first frame unshown until something
        // redraws the view, so it is redrawn once the page has had time to paint.
        Canvas.NavigationCompleted += async (_, _) =>
        {
            await Task.Delay(Settle);
            Canvas.InvalidateVisual();
        };
    }

    public static void Dress(Frame frame, PaintingViewModel painting)
    {
        frame.Bar.DataContext = painting;
        frame.Bar.Bind(TitleBar.SubtitleProperty, new Binding(nameof(PaintingViewModel.Title)));
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is PaintingViewModel painting)
        {
            painting.PropertyChanged += Changed;
            Paint();
        }
    }

    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PaintingViewModel.Html))
        {
            Paint();
        }
    }

    private void Paint()
    {
        if (DataContext is PaintingViewModel { Html: { } html })
        {
            Canvas.NavigateToString(html);
        }
    }
}
