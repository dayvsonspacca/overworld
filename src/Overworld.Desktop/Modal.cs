using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Overworld.Desktop;

[TemplatePart("PART_Backdrop", typeof(Control))]
[TemplatePart("PART_Close", typeof(Button))]
[PseudoClasses(":compact")]
public sealed class Modal : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Modal, string?>(nameof(Title));

    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<Modal, bool>(nameof(IsCompact));

    public static readonly RoutedEvent<RoutedEventArgs> DismissedEvent =
        RoutedEvent.Register<Modal, RoutedEventArgs>(nameof(Dismissed), RoutingStrategies.Bubble);

    public Modal() => AddHandler(Button.ClickEvent, Clicked);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCompactProperty)
        {
            PseudoClasses.Set(":compact", IsCompact);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (IsPart(e.Source, "PART_Backdrop"))
        {
            Dismiss(e);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            Dismiss(e);
        }
    }

    private void Clicked(object? sender, RoutedEventArgs e)
    {
        if (IsPart(e.Source, "PART_Close"))
        {
            Dismiss(e);
        }
    }

    private bool IsPart(object? source, string name) =>
        source is Control control && control.Name == name && control.TemplatedParent == this;

    private void Dismiss(RoutedEventArgs e)
    {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(DismissedEvent, this));
    }
}
