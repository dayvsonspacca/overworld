using Avalonia;
using Avalonia.Controls;

namespace Overworld.Apps.Crafter.Desktop.Conversations;

public sealed class TimelineNode : ContentControl
{
    public static readonly StyledProperty<string?> GlyphProperty =
        AvaloniaProperty.Register<TimelineNode, string?>(nameof(Glyph));

    public string? Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
}
