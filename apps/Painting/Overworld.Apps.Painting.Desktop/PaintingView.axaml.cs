using Avalonia.Controls;
using Avalonia.Data;
using Overworld.Apps.Painting.Presentation;
using Overworld.Desktop;
using Overworld.Desktop.Windowing;

namespace Overworld.Apps.Painting.Desktop;

public sealed partial class PaintingView : UserControl
{
    public PaintingView() => InitializeComponent();

    public static void Dress(Frame frame, PaintingViewModel painting)
    {
        frame.Bar.DataContext = painting;
        frame.Bar.Bind(TitleBar.SubtitleProperty, new Binding(nameof(PaintingViewModel.Title)));
    }
}
