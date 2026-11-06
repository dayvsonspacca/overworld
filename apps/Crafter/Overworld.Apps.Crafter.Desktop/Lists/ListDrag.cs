using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;

namespace Overworld.Apps.Crafter.Desktop.Lists;

public sealed class ListDrag
{
    private const double Slack = 6;

    private const double Arm = 0.4;

    private readonly ItemsControl _list;

    private Gesture? _gesture;

    public ListDrag(ItemsControl list)
    {
        _list = list;
        list.AddHandler(InputElement.PointerPressedEvent, Press, RoutingStrategies.Tunnel, handledEventsToo: true);
        list.AddHandler(InputElement.PointerMovedEvent, Drag, RoutingStrategies.Tunnel, handledEventsToo: true);
        list.AddHandler(InputElement.PointerReleasedEvent, Drop, RoutingStrategies.Tunnel, handledEventsToo: true);
        list.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => Cancel());
    }

    public Func<bool> CanLift { get; init; } = () => true;

    public Func<int, int, Task>? Arranged { get; init; }

    public Action<int>? Swiped { get; init; }

    private void Press(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_list).Properties.IsLeftButtonPressed || Container(e.Source as Visual) is not { } container)
        {
            return;
        }

        _gesture = new Gesture(container, _list.IndexFromContainer(container), e.GetPosition(_list));
    }

    private void Drag(object? sender, PointerEventArgs e)
    {
        if (_gesture is not { } gesture)
        {
            return;
        }

        var delta = e.GetPosition(_list) - gesture.Start;

        if (gesture.Kind == Kind.Pending)
        {
            if (Math.Abs(delta.X) < Slack && Math.Abs(delta.Y) < Slack)
            {
                return;
            }

            gesture.Kind = Math.Abs(delta.X) > Math.Abs(delta.Y)
                ? delta.X < 0 && Swiped is not null ? Kind.Swipe : Kind.None
                : Arranged is not null && CanLift() ? Kind.Lift : Kind.None;

            if (gesture.Kind == Kind.None)
            {
                _gesture = null;

                return;
            }

            e.Pointer.Capture(_list);
            gesture.Container.Classes.Add(gesture.Kind == Kind.Swipe ? "swiping" : "lifted");

            if (gesture.Kind == Kind.Lift)
            {
                gesture.Container.ZIndex = 1;
                _list.Classes.Add("arranging");
            }
        }

        if (gesture.Kind == Kind.Swipe)
        {
            Swipe(gesture, delta.X);
        }
        else
        {
            Lift(gesture, delta.Y);
        }

        e.Handled = true;
    }

    private static void Swipe(Gesture gesture, double dx)
    {
        var left = Math.Min(0, dx);
        var share = gesture.Container.Bounds.Width > 0 ? -left / gesture.Container.Bounds.Width : 0;

        if (Part(gesture.Container, "swipe-card") is { } card)
        {
            card.RenderTransform = Moved(left, 0);
        }

        if (Part(gesture.Container, "swipe-bin") is { } bin)
        {
            bin.Opacity = Math.Min(1, share * 3);
        }

        gesture.Armed = share >= Arm;

        if (gesture.Armed)
        {
            gesture.Container.Classes.Add("armed");
        }
        else
        {
            gesture.Container.Classes.Remove("armed");
        }
    }

    private void Lift(Gesture gesture, double dy)
    {
        var rows = _list.GetRealizedContainers().OrderBy(_list.IndexFromContainer).ToList();
        var lifted = gesture.Container.Bounds;
        var at = rows.IndexOf(gesture.Container);
        var step = at + 1 < rows.Count ? rows[at + 1].Bounds.Top - lifted.Top
            : at > 0 ? lifted.Top - rows[at - 1].Bounds.Top
            : 0;
        var y = Math.Clamp(dy, rows[0].Bounds.Top - lifted.Top, rows[^1].Bounds.Top - lifted.Top);
        var center = lifted.Center.Y + y;

        gesture.Container.RenderTransform = Moved(0, y);
        gesture.To = gesture.Index;

        foreach (var row in rows.Where(row => row != gesture.Container))
        {
            var index = _list.IndexFromContainer(row);
            var shift = index > gesture.Index && row.Bounds.Center.Y < center ? -step
                : index < gesture.Index && row.Bounds.Center.Y > center ? step
                : 0;

            gesture.To += Math.Sign(-shift);
            row.RenderTransform = Moved(0, shift);
        }
    }

    private async void Drop(object? sender, PointerReleasedEventArgs e)
    {
        if (_gesture is not { } gesture)
        {
            return;
        }

        _gesture = null;

        if (gesture.Kind == Kind.Pending)
        {
            return;
        }

        e.Handled = true;
        Reset(gesture);
        e.Pointer.Capture(null);

        if (gesture.Kind == Kind.Swipe && gesture.Armed)
        {
            Swiped?.Invoke(gesture.Index);
        }
        else if (gesture.Kind == Kind.Lift && gesture.To != gesture.Index && Arranged is { } arranged)
        {
            await arranged(gesture.Index, gesture.To);
        }
    }

    private void Cancel()
    {
        if (_gesture is { Kind: not Kind.Pending } gesture)
        {
            Reset(gesture);
        }

        _gesture = null;
    }

    private void Reset(Gesture gesture)
    {
        _list.Classes.Remove("arranging");

        foreach (var row in _list.GetRealizedContainers())
        {
            row.RenderTransform = null;
        }

        gesture.Container.ZIndex = 0;
        gesture.Container.Classes.RemoveAll(["swiping", "armed", "lifted"]);
        Part(gesture.Container, "swipe-card")?.ClearValue(Visual.RenderTransformProperty);
        Part(gesture.Container, "swipe-bin")?.ClearValue(Visual.OpacityProperty);
    }

    private Control? Container(Visual? source)
    {
        for (var visual = source; visual is not null && visual != _list; visual = visual.GetVisualParent())
        {
            if (visual is Control control && _list.IndexFromContainer(control) >= 0)
            {
                return control;
            }
        }

        return null;
    }

    private static Control? Part(Control container, string name) =>
        container.GetVisualDescendants().OfType<Control>().FirstOrDefault(part => part.Classes.Contains(name));

    private static TransformOperations Moved(double x, double y) => TransformOperations.Parse($"translate({x}px, {y}px)");

    private enum Kind
    {
        Pending,
        Swipe,
        Lift,
        None,
    }

    private sealed class Gesture(Control container, int index, Point start)
    {
        public Control Container { get; } = container;

        public int Index { get; } = index;

        public Point Start { get; } = start;

        public Kind Kind { get; set; }

        public int To { get; set; }

        public bool Armed { get; set; }
    }
}
