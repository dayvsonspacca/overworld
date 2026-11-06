using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Overworld.Desktop.Windowing;

[TemplatePart("PART_Minimize", typeof(Button))]
[TemplatePart("PART_Maximize", typeof(Button))]
[TemplatePart("PART_Close", typeof(Button))]
[PseudoClasses(":tiles", ":maximized")]
public sealed class TitleBar : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Subtitle));

    public static readonly StyledProperty<object?> LeadingProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(Leading));

    public static readonly StyledProperty<object?> TrailingProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(Trailing));

    public static readonly StyledProperty<IImage?> IconProperty =
        AvaloniaProperty.Register<TitleBar, IImage?>(nameof(Icon));

    public static readonly StyledProperty<WindowManager> ManagerProperty =
        AvaloniaProperty.Register<TitleBar, WindowManager>(nameof(Manager), WindowManager.Detect());

    private Window? _window;

    public TitleBar()
    {
        PseudoClasses.Set(":tiles", Manager.Tiles);
        AddHandler(Button.ClickEvent, Clicked);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

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

    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public WindowManager Manager
    {
        get => GetValue(ManagerProperty);
        set => SetValue(ManagerProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ManagerProperty)
        {
            PseudoClasses.Set(":tiles", Manager.Tiles);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _window = TopLevel.GetTopLevel(this) as Window;

        if (_window is not null)
        {
            _window.PropertyChanged += WindowChanged;
            Follow(_window.WindowState);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_window is not null)
        {
            _window.PropertyChanged -= WindowChanged;
            _window = null;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (_window is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            if (!Manager.Tiles)
            {
                ToggleMaximized(_window);
            }

            return;
        }

        _window.BeginMoveDrag(e);
    }

    private void Clicked(object? sender, RoutedEventArgs e)
    {
        if (_window is null)
        {
            return;
        }

        switch ((e.Source as Control)?.Name)
        {
            case "PART_Minimize":
                _window.WindowState = WindowState.Minimized;
                break;
            case "PART_Maximize":
                ToggleMaximized(_window);
                break;
            case "PART_Close":
                _window.Close();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void WindowChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == Window.WindowStateProperty)
        {
            Follow(_window!.WindowState);
        }
    }

    private void Follow(WindowState state) => PseudoClasses.Set(":maximized", state == WindowState.Maximized);

    private static void ToggleMaximized(Window window) =>
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
}
