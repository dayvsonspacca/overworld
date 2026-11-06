using Avalonia;
using Avalonia.Controls.Primitives;

namespace Overworld.Desktop;

public sealed class StatusBar : TemplatedControl
{
    public static readonly StyledProperty<object?> LeadingProperty =
        AvaloniaProperty.Register<StatusBar, object?>(nameof(Leading));

    public static readonly StyledProperty<object?> TrailingProperty =
        AvaloniaProperty.Register<StatusBar, object?>(nameof(Trailing));

    public object? Leading
    {
        get => GetValue(LeadingProperty);
        set => SetValue(LeadingProperty, value);
    }

    public object? Trailing
    {
        get => GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }
}
