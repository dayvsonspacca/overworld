using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Overworld.Desktop.Text;

namespace Overworld.Desktop.Tests.Text;

internal sealed class Page
{
    private Page(IEnumerable<Control> content)
    {
        var column = new StackPanel { Spacing = 24, Margin = new Thickness(24) };
        column.Children.AddRange(content);
        Scroller = new TextPage { Content = column };
        Window = new Window { Width = 800, Height = 600, Content = Scroller };
        Window.Show();
        Settle();
    }

    public Window Window { get; }

    public TextPage Scroller { get; }

    public TextSelection Selection => Scroller.Selection;

    public static Page With(params Control[] content) => new(content);

    public static SelectableText Paragraph(string text) => new() { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    public static SelectableText Bold(string bold, string plain)
    {
        var block = new SelectableText();
        block.Inlines!.Add(new Run(bold) { FontWeight = Avalonia.Media.FontWeight.Bold });
        block.Inlines.Add(new Run(plain));

        return block;
    }

    public static Control List(params string[] items)
    {
        var group = new object();
        var list = new StackPanel { Spacing = 4 };

        foreach (var item in items)
        {
            var row = new object();
            var line = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            line.Children.Add(new SelectableText { Text = "•", IsMarker = true, Group = group, Row = row });
            line.Children.Add(new SelectableText { Text = item, Group = group, Row = row });
            list.Children.Add(line);
        }

        return list;
    }

    public SelectableText Block(string containing) =>
        Window.GetVisualDescendants().OfType<SelectableText>().First(block => !block.IsMarker && block.Plain.Contains(containing, StringComparison.Ordinal));

    public SelectableText Marker(string item)
    {
        var text = Block(item);

        return Window.GetVisualDescendants().OfType<SelectableText>().First(block => block.IsMarker && block.Row == text.Row);
    }

    public Point Before(string piece, string? within = null) => Edge(within ?? piece, piece, after: false);

    public Point After(string piece, string? within = null) => Edge(within ?? piece, piece, after: true);

    public Point LeftOf(SelectableText block) => block.TranslatePoint(new Point(-3, block.Bounds.Height / 2), Window)!.Value;

    public void Drag(Point from, Point to)
    {
        Window.MouseDown(from, MouseButton.Left);
        Window.MouseMove(new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2), RawInputModifiers.LeftMouseButton);
        Window.MouseMove(to, RawInputModifiers.LeftMouseButton);
        Window.MouseUp(to, MouseButton.Left);
        Settle();
    }

    public void Click(Point at, int times = 1, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        for (var click = 0; click < times; click++)
        {
            Window.MouseDown(at, MouseButton.Left, modifiers);
            Window.MouseUp(at, MouseButton.Left, modifiers);
        }

        Settle();
    }

    public void Press(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.Control)
    {
        Window.KeyPressQwerty(key, modifiers);
        Settle();
    }

    public string? Copied()
    {
        Press(PhysicalKey.C);

        return Window.Clipboard!.TryGetTextAsync().GetAwaiter().GetResult();
    }

    public void Paste(string text)
    {
        Window.Clipboard!.SetTextAsync(text).GetAwaiter().GetResult();
        Settle();
    }

    public static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private Point Edge(string within, string piece, bool after)
    {
        var block = Block(within);
        var index = block.Plain.IndexOf(piece, StringComparison.Ordinal) + (after ? piece.Length : 0);
        var caret = block.TextLayout.HitTestTextPosition(index);
        var local = new Point(after ? caret.X - 1 : caret.X + 1, caret.Y + (caret.Height / 2));

        return block.TranslatePoint(local, Window)!.Value;
    }
}
