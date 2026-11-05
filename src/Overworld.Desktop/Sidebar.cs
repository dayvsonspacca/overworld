using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Overworld.Desktop;

public sealed class Sidebar : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Panel.BackgroundProperty.AddOwner<Sidebar>();

    public static readonly StyledProperty<IBrush?> LineProperty =
        AvaloniaProperty.Register<Sidebar, IBrush?>(nameof(Line));

    static Sidebar()
    {
        AffectsRender<Sidebar>(BackgroundProperty, LineProperty);
        SidebarSection.FillProperty.Changed.AddClassHandler<SidebarSection>(
            (section, _) => (section.Parent as Sidebar)?.InvalidateMeasure());
    }

    public Sidebar() => Children.CollectionChanged += ChildrenChanged;

    [Content]
    public Controls Children { get; } = [];

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? Line
    {
        get => GetValue(LineProperty);
        set => SetValue(LineProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var fill = Filling();
        var width = 0.0;
        var height = 0.0;

        foreach (var child in Children.Where(child => child != fill))
        {
            child.Measure(availableSize.WithHeight(double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        if (fill is not null)
        {
            fill.Measure(availableSize.WithHeight(Math.Max(0, availableSize.Height - height)));
            width = Math.Max(width, fill.DesiredSize.Width);
            height += fill.DesiredSize.Height;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var fill = Filling();
        var rest = finalSize.Height - Children.Where(child => child != fill).Sum(child => child.DesiredSize.Height);
        var top = 0.0;

        foreach (var child in Children)
        {
            var height = child == fill ? Math.Max(0, rest) : child.DesiredSize.Height;

            child.Arrange(new Rect(0, top, finalSize.Width, height));
            top += height;
        }

        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        if (Background is not null)
        {
            context.FillRectangle(Background, new Rect(Bounds.Size));
        }

        if (Line is null)
        {
            return;
        }

        var pen = new Pen(Line);
        var right = Bounds.Width - 0.5;

        context.DrawLine(pen, new Point(right, 0), new Point(right, Bounds.Height));

        foreach (var child in Children.Where(child => child.IsVisible).Skip(1))
        {
            var y = Math.Round(child.Bounds.Top) + 0.5;

            context.DrawLine(pen, new Point(0, y), new Point(right, y));
        }
    }

    private Control? Filling() =>
        Children.FirstOrDefault(child => child is SidebarSection { Fill: true, IsVisible: true });

    private void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs change)
    {
        foreach (var child in change.OldItems?.OfType<Control>() ?? [])
        {
            LogicalChildren.Remove(child);
            VisualChildren.Remove(child);
        }

        foreach (var child in change.NewItems?.OfType<Control>() ?? [])
        {
            LogicalChildren.Add(child);
            VisualChildren.Add(child);
        }

        InvalidateMeasure();
    }
}
