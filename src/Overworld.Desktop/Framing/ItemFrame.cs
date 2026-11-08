using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Overworld.Desktop.Framing;

[TemplatePart("PART_Stage", typeof(ContentPresenter))]
public sealed class ItemFrame : TemplatedControl
{
    public const double Reach = 240;

    private const double Notch = 50;

    public static readonly StyledProperty<object?> ItemProperty =
        AvaloniaProperty.Register<ItemFrame, object?>(nameof(Item));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<ItemFrame, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ItemFrame, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<ItemFrame, string?>(nameof(Source));

    public static readonly StyledProperty<double> StageHeightProperty =
        AvaloniaProperty.Register<ItemFrame, double>(nameof(StageHeight), defaultValue: 320);

    public static readonly StyledProperty<ICommand?> ExpandCommandProperty =
        AvaloniaProperty.Register<ItemFrame, ICommand?>(nameof(ExpandCommand));

    public static readonly StyledProperty<object?> ExpandCommandParameterProperty =
        AvaloniaProperty.Register<ItemFrame, object?>(nameof(ExpandCommandParameter));

    public static readonly DirectProperty<ItemFrame, bool> IsSeenProperty =
        AvaloniaProperty.RegisterDirect<ItemFrame, bool>(
            nameof(IsSeen),
            frame => frame.IsSeen,
            (_, _) => { },
            defaultBindingMode: BindingMode.OneWayToSource);

    private readonly DispatcherTimer _lingering;

    private readonly DispatcherTimer _waking;

    private bool _placed;

    private long _nearSince;

    private bool _isNear;

    private Visual[] _holders = [];

    private ContentPresenter? _stage;

    private bool _seen;

    public ItemFrame()
    {
        _lingering = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _lingering.Tick += (_, _) =>
        {
            _lingering.Stop();
            Show(false);
        };
        _waking = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _waking.Tick += (_, _) =>
        {
            _waking.Stop();
            Show(true);
        };
        EffectiveViewportChanged += Moved;
        AddHandler(PointerWheelChangedEvent, Wheeled, RoutingStrategies.Tunnel);
    }

    [Content]
    public object? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double StageHeight
    {
        get => GetValue(StageHeightProperty);
        set => SetValue(StageHeightProperty, value);
    }

    public ICommand? ExpandCommand
    {
        get => GetValue(ExpandCommandProperty);
        set => SetValue(ExpandCommandProperty, value);
    }

    public object? ExpandCommandParameter
    {
        get => GetValue(ExpandCommandParameterProperty);
        set => SetValue(ExpandCommandParameterProperty, value);
    }

    internal TimeSpan Linger
    {
        get => _lingering.Interval;
        set => _lingering.Interval = value;
    }

    internal TimeSpan Wake
    {
        get => _waking.Interval;
        set => _waking.Interval = value;
    }

    public bool IsSeen
    {
        get => _seen;
        private set => SetAndRaise(IsSeenProperty, ref _seen, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _stage = e.NameScope.Find<ContentPresenter>("PART_Stage");
        Stage();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemProperty)
        {
            Stage();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _holders = [.. this.GetVisualAncestors()];

        foreach (var holder in _holders)
        {
            holder.PropertyChanged += Held;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        foreach (var holder in _holders)
        {
            holder.PropertyChanged -= Held;
        }

        _holders = [];
        _lingering.Stop();
        _waking.Stop();
        _placed = false;
        _isNear = false;
        Show(false);
    }

    private void Moved(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var near = e.EffectiveViewport.Inflate(Reach).Intersects(new Rect(Bounds.Size));
        var first = !_placed;
        _placed = true;
        _isNear = near;

        if (!IsEffectivelyVisible)
        {
            return;
        }

        if (near)
        {
            _lingering.Stop();

            if (first)
            {
                Show(true);
            }
            else if (!IsSeen && !_waking.IsEnabled)
            {
                _nearSince = Stopwatch.GetTimestamp();
                _waking.Start();
            }
            else if (!IsSeen && Stopwatch.GetElapsedTime(_nearSince) < Wake * 4)
            {
                _waking.Stop();
                _waking.Start();
            }
        }
        else
        {
            _waking.Stop();

            if (IsSeen && !_lingering.IsEnabled)
            {
                _lingering.Start();
            }
        }
    }

    private void Held(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != IsVisibleProperty)
        {
            return;
        }

        _waking.Stop();

        if (!IsEffectivelyVisible)
        {
            if (IsSeen && !_lingering.IsEnabled)
            {
                _lingering.Start();
            }
        }
        else if (_isNear)
        {
            _lingering.Stop();
            Show(true);
        }
    }

    private void Wheeled(object? sender, PointerWheelEventArgs e)
    {
        if (this.FindAncestorOfType<ScrollViewer>() is not { } scroll)
        {
            return;
        }

        scroll.Offset = new Vector(scroll.Offset.X - (e.Delta.X * Notch), scroll.Offset.Y - (e.Delta.Y * Notch));
        e.Handled = true;
    }

    private void Show(bool seen)
    {
        if (IsSeen == seen)
        {
            return;
        }

        IsSeen = seen;
        Stage();
    }

    private void Stage()
    {
        if (_stage is not null)
        {
            _stage.Content = IsSeen ? Item : null;
        }
    }
}
