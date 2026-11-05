using Avalonia;
using Avalonia.Controls;

namespace Overworld.Desktop;

public sealed class SidebarSection : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SidebarSection, string?>(nameof(Title));

    public static readonly StyledProperty<string?> CountProperty =
        AvaloniaProperty.Register<SidebarSection, string?>(nameof(Count));

    public static readonly StyledProperty<bool> FillProperty =
        AvaloniaProperty.Register<SidebarSection, bool>(nameof(Fill));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public bool Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }
}
