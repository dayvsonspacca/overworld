using Avalonia.Controls;
using Avalonia.Input;

namespace Overworld.Desktop.Text;

public class SelectableText : SelectableTextBlock
{
    public SelectableText()
    {
        Focusable = false;
        ContextFlyout = null;
    }

    public object? Group { get; set; }

    public object? Row { get; set; }

    public bool IsMarker { get; set; }

    public IReadOnlyList<LinkSpan> Links { get; set; } = [];

    public string Plain => (Inlines is { Count: > 0 } inlines ? inlines.Text : Text) ?? string.Empty;

    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
    }
}
