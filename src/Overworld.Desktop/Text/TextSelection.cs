using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Overworld.Desktop.Text;
public sealed class TextSelection
{
    private const double DragDistance = 3;

    private const double SameRow = 1;

    private static Cursor? _hand;

    private readonly ScrollViewer _scroller;

    private readonly DispatcherTimer _edgeScroll;

    private readonly MenuItem _copyItem;

    private readonly ContextMenu _menu;

    private HashSet<SelectableText> _lit = [];

    private Caret? _anchor;

    private Caret? _focus;

    private Picked? _picked;

    private bool _pressing;

    private bool _dragged;

    private Point _pressedAt;

    private Point _pointerAt;

    private string? _pressedLink;

    private TextSelection(ScrollViewer scroller)
    {
        _scroller = scroller;
        scroller.Focusable = true;
        scroller.AddHandler(InputElement.PointerPressedEvent, Press, RoutingStrategies.Tunnel);
        scroller.AddHandler(InputElement.PointerMovedEvent, Move, RoutingStrategies.Tunnel, handledEventsToo: true);
        scroller.AddHandler(InputElement.PointerReleasedEvent, Release, RoutingStrategies.Tunnel, handledEventsToo: true);
        scroller.AddHandler(InputElement.KeyDownEvent, Command, RoutingStrategies.Bubble);
        scroller.AddHandler(Control.ContextRequestedEvent, Context, RoutingStrategies.Bubble);

        _edgeScroll = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Input, Edge);

        _copyItem = new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        _copyItem.Click += (_, _) => Copy();
        var all = new MenuItem { Header = "Select all", InputGesture = new KeyGesture(Key.A, KeyModifiers.Control) };
        all.Click += (_, _) => SelectAll();
        _menu = new ContextMenu { ItemsSource = new[] { _copyItem, all } };
    }

    public bool HasSelection => _lit.Count > 0;
    public string SelectedText
    {
        get
        {
            var builder = new StringBuilder();
            SelectableText? previous = null;

            foreach (var block in Place().Select(placed => placed.Block).Where(_lit.Contains))
            {
                var plain = block.Plain;
                var start = Math.Clamp(Math.Min(block.SelectionStart, block.SelectionEnd), 0, plain.Length);
                var end = Math.Clamp(Math.Max(block.SelectionStart, block.SelectionEnd), 0, plain.Length);
                if (end <= start)
                {
                    continue;
                }

                if (previous is not null)
                {
                    builder.Append(Between(previous, block));
                }

                builder.Append(plain, start, end - start);
                previous = block;
            }

            return builder.ToString();
        }
    }

    internal static TextSelection Watch(ScrollViewer scroller) => new(scroller);

    public void Clear()
    {
        _anchor = null;
        _focus = null;
        _picked = null;
        Light(null);
    }

    public void SelectAll()
    {
        var placed = Place();
        if (placed.Count == 0)
        {
            return;
        }

        _picked = null;
        Take();
        _anchor = new Caret(placed[0].Block, 0);
        _focus = new Caret(placed[^1].Block, placed[^1].Block.Plain.Length);
        Light(placed);
    }

    public void Copy()
    {
        var text = SelectedText;
        if (text.Length > 0 && TopLevel.GetTopLevel(_scroller)?.Clipboard is { } clipboard)
        {
            _ = clipboard.SetTextAsync(text);
        }
    }
    internal static (int Start, int End)? WordAround(string text, int character)
    {
        if (text.Length == 0)
        {
            return null;
        }

        var at = Math.Clamp(character, 0, text.Length - 1);
        var kind = KindOf(text[at]);
        if (kind == CharKind.Other)
        {
            return (at, at + 1);
        }

        var start = at;
        while (start > 0 && KindOf(text[start - 1]) == kind)
        {
            start--;
        }

        var end = at + 1;
        while (end < text.Length && KindOf(text[end]) == kind)
        {
            end++;
        }

        return (start, end);
    }

    private static string Between(SelectableText previous, SelectableText next)
    {
        if (next.Row is not null && next.Row == previous.Row)
        {
            return previous.IsMarker ? " " : "\t";
        }

        return next.Group is not null && next.Group == previous.Group ? "\n" : "\n\n";
    }

    private void Press(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_scroller);
        if (!point.Properties.IsLeftButtonPressed || IsControl(e.Source) || _scroller.Content is not Visual content)
        {
            return;
        }

        var placed = Place();
        var at = e.GetPosition(content);
        if (Locate(at, placed) is not { } caret)
        {
            return;
        }

        e.Handled = true;
        e.Pointer.Capture(_scroller);
        _pressing = true;
        _dragged = false;
        _pressedAt = _pointerAt = point.Position;
        _pressedLink = null;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _anchor is not null)
        {
            _picked = null;
            _focus = caret;
        }
        else if (e.ClickCount == 2 && WordAround(caret.Block.Plain, CharacterAt(placed, caret, at)) is var (start, end))
        {
            _picked = new Picked(caret with { At = start }, caret with { At = end });
            (_anchor, _focus) = (_picked.Value.Start, _picked.Value.End);
        }
        else if (e.ClickCount >= 3)
        {
            _picked = new Picked(caret with { At = 0 }, caret with { At = caret.Block.Plain.Length });
            (_anchor, _focus) = (_picked.Value.Start, _picked.Value.End);
        }
        else
        {
            _picked = null;
            _anchor = _focus = caret;
            _pressedLink = LinkUnder(placed, at);
        }
        if (_picked is not null || _anchor != _focus)
        {
            Take();
        }

        Light(placed);
    }

    private void Move(object? sender, PointerEventArgs e)
    {
        if (!_pressing)
        {
            Hover(e);

            return;
        }

        _pointerAt = e.GetPosition(_scroller);
        var moved = _pointerAt - _pressedAt;
        if (!_dragged && Math.Abs(moved.X) < DragDistance && Math.Abs(moved.Y) < DragDistance)
        {
            return;
        }

        _dragged = true;
        _pressedLink = null;
        Take();
        Extend();

        if (_pointerAt.Y < 0 || _pointerAt.Y > _scroller.Viewport.Height)
        {
            _edgeScroll.Start();
        }
        else
        {
            _edgeScroll.Stop();
        }
    }

    private void Release(object? sender, PointerReleasedEventArgs e)
    {
        if (!_pressing)
        {
            return;
        }

        _pressing = false;
        _edgeScroll.Stop();
        e.Pointer.Capture(null);
        e.Handled = true;

        if (!_dragged && _pressedLink is { } url)
        {
            Open(url);
        }
    }

    private void Take() => _scroller.Focus(NavigationMethod.Pointer);

    private void Extend()
    {
        if (_scroller.Content is not Visual content || _scroller.TranslatePoint(_pointerAt, content) is not { } at)
        {
            return;
        }

        var placed = Place();
        if (Locate(at, placed) is not { } caret)
        {
            return;
        }

        if (_picked is { } unit)
        {
            if (Compare(caret, unit.Start, placed) < 0)
            {
                (_anchor, _focus) = (unit.End, caret);
            }
            else
            {
                (_anchor, _focus) = (unit.Start, Compare(caret, unit.End, placed) > 0 ? caret : unit.End);
            }
        }
        else
        {
            _focus = caret;
        }

        Light(placed);
    }
    private void Edge(object? sender, EventArgs e)
    {
        var height = _scroller.Viewport.Height;
        var beyond = _pointerAt.Y < 0 ? _pointerAt.Y : _pointerAt.Y > height ? _pointerAt.Y - height : 0;
        if (!_pressing || beyond == 0)
        {
            _edgeScroll.Stop();

            return;
        }

        var top = Math.Max(0, _scroller.Extent.Height - height);
        _scroller.Offset = new Vector(_scroller.Offset.X, Math.Clamp(_scroller.Offset.Y + (beyond / 2), 0, top));
        Extend();
    }

    private static void Hover(PointerEventArgs e)
    {
        if (e.Source is not SelectableText { Links.Count: > 0 } block)
        {
            return;
        }

        var hit = block.TextLayout.HitTestPoint(e.GetPosition(block));
        var index = hit.CharacterHit.FirstCharacterIndex;
        if (hit.IsInside && block.Links.Any(link => index >= link.Start && index < link.End))
        {
            block.Cursor = _hand ??= new Cursor(StandardCursorType.Hand);
        }
        else
        {
            block.ClearValue(InputElement.CursorProperty);
        }
    }

    private void Command(object? sender, KeyEventArgs e)
    {
        var command = e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta;

        if (command && e.Key == Key.C && HasSelection)
        {
            e.Handled = true;
            Copy();
        }
        else if (command && e.Key == Key.A)
        {
            e.Handled = true;
            SelectAll();
        }
        else if (e.Key == Key.Escape && HasSelection)
        {
            e.Handled = true;
            Clear();
        }
    }

    private void Context(object? sender, ContextRequestedEventArgs e)
    {
        if (IsControl(e.Source))
        {
            return;
        }

        e.Handled = true;
        _copyItem.IsEnabled = HasSelection;
        _menu.Open(_scroller);
    }

    private void Open(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto")
        {
            _ = TopLevel.GetTopLevel(_scroller)?.Launcher.LaunchUriAsync(uri);
        }
    }
    private bool IsControl(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != _scroller; visual = visual.GetVisualParent())
        {
            if (visual is Button or TextBox or ScrollBar or Thumb or ComboBox or Slider or MenuItem)
            {
                return true;
            }
        }

        return false;
    }

    private void Light(List<Placed>? placed)
    {
        HashSet<SelectableText> now = [];

        if (placed is not null && _anchor is { } one && _focus is { } other)
        {
            var (from, to) = Compare(one, other, placed) <= 0 ? (one, other) : (other, one);
            var first = IndexOf(placed, from.Block);
            var last = IndexOf(placed, to.Block);

            for (var at = first; first >= 0 && last >= 0 && at <= last; at++)
            {
                var block = placed[at].Block;
                var start = at == first ? from.At : 0;
                var end = at == last ? to.At : block.Plain.Length;
                if (start == end)
                {
                    continue;
                }

                block.SelectionStart = start;
                block.SelectionEnd = end;
                now.Add(block);
            }
        }

        foreach (var block in _lit.Where(block => !now.Contains(block)))
        {
            block.SelectionStart = 0;
            block.SelectionEnd = 0;
        }

        _lit = now;
    }
    private List<Placed> Place()
    {
        List<Placed> placed = [];
        if (_scroller.Content is not Visual content)
        {
            return placed;
        }

        foreach (var block in content.GetVisualDescendants().OfType<SelectableText>())
        {
            if (block.IsEffectivelyVisible && block.Bounds.Width > 0 && block.TranslatePoint(default, content) is { } origin)
            {
                placed.Add(new Placed(block, new Rect(origin, block.Bounds.Size)));
            }
        }

        placed.Sort(static (one, other) => Math.Abs(one.Bounds.Top - other.Bounds.Top) < SameRow
            ? one.Bounds.Left.CompareTo(other.Bounds.Left)
            : one.Bounds.Top.CompareTo(other.Bounds.Top));

        return placed;
    }
    private static Caret? Locate(Point point, List<Placed> placed)
    {
        Placed? nearest = null;
        var distance = double.PositiveInfinity;

        foreach (var each in placed.Where(each => point.Y >= each.Bounds.Top && point.Y < each.Bounds.Bottom))
        {
            var away = point.X < each.Bounds.Left ? each.Bounds.Left - point.X
                : point.X > each.Bounds.Right ? point.X - each.Bounds.Right
                : 0;
            if (away < distance)
            {
                (nearest, distance) = (each, away);
            }
        }

        if (nearest is { } hit)
        {
            var position = hit.Block.TextLayout.HitTestPoint(point - hit.Bounds.TopLeft).TextPosition;

            return new Caret(hit.Block, Math.Clamp(position, 0, hit.Block.Plain.Length));
        }

        var above = placed.LastOrDefault(each => each.Bounds.Bottom <= point.Y);
        if (above.Block is not null)
        {
            return new Caret(above.Block, above.Block.Plain.Length);
        }

        return placed.Count > 0 ? new Caret(placed[0].Block, 0) : null;
    }
    private static int CharacterAt(List<Placed> placed, Caret caret, Point point)
    {
        var bounds = placed[IndexOf(placed, caret.Block)].Bounds;
        var hit = caret.Block.TextLayout.HitTestPoint(point - bounds.TopLeft);

        return hit.IsInside ? hit.CharacterHit.FirstCharacterIndex : caret.At - 1;
    }

    private static string? LinkUnder(List<Placed> placed, Point point)
    {
        foreach (var each in placed.Where(each => each.Block.Links.Count > 0 && each.Bounds.Contains(point)))
        {
            var hit = each.Block.TextLayout.HitTestPoint(point - each.Bounds.TopLeft);
            var index = hit.CharacterHit.FirstCharacterIndex;
            if (hit.IsInside)
            {
                return each.Block.Links.FirstOrDefault(link => index >= link.Start && index < link.End)?.Url;
            }
        }

        return null;
    }

    private static int Compare(Caret one, Caret other, List<Placed> placed)
    {
        var order = IndexOf(placed, one.Block).CompareTo(IndexOf(placed, other.Block));

        return order != 0 ? order : one.At.CompareTo(other.At);
    }

    private static int IndexOf(List<Placed> placed, SelectableText block) => placed.FindIndex(each => each.Block == block);

    private static CharKind KindOf(char c) =>
        char.IsLetterOrDigit(c) || c == '_' ? CharKind.Word
        : char.IsWhiteSpace(c) ? CharKind.Space
        : CharKind.Other;

    private enum CharKind
    {
        Word,
        Space,
        Other,
    }

    private readonly record struct Caret(SelectableText Block, int At);

    private readonly record struct Picked(Caret Start, Caret End);

    private readonly record struct Placed(SelectableText Block, Rect Bounds);
}
