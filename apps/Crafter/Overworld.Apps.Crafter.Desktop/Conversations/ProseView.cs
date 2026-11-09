using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Overworld.Apps.Crafter.Presentation.Highlight;
using Overworld.Desktop.Text;
using Md = Overworld.Apps.Crafter.Presentation.Markdown;

namespace Overworld.Apps.Crafter.Desktop.Conversations;

public sealed class ProseView : Decorator
{
    public static readonly StyledProperty<IReadOnlyList<Md.Block>> BlocksProperty =
        AvaloniaProperty.Register<ProseView, IReadOnlyList<Md.Block>>(nameof(Blocks), []);

    private const double BlockSpacing = 12;

    private const double ItemSpacing = 6;

    private static readonly string[] Bullets = ["•", "◦", "▪"];

    private static readonly Dictionary<Shade, string> Shades = new()
    {
        [Shade.Keyword] = "AccentTextBrush",
        [Shade.Type] = "AccentLightBrush",
        [Shade.Text] = "AddedBrush",
        [Shade.Number] = "DangerBrush",
        [Shade.Comment] = "SubtleBrush",
        [Shade.Attribute] = "AccentLightBrush",
    };

    private readonly StackPanel _page = new() { Spacing = BlockSpacing };

    private readonly List<Md.Block> _drawn = [];

    private bool _redrawing;

    public ProseView() => Child = _page;

    public IReadOnlyList<Md.Block> Blocks
    {
        get => GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != BlocksProperty)
        {
            return;
        }

        // An answer streams in dozens of times a second; what arrives together is drawn once,
        // after it has all come in, so the window keeps up instead of falling behind.
        if (_drawn.Count == 0 || TopLevel.GetTopLevel(this) is null)
        {
            Redraw();
        }
        else if (!_redrawing)
        {
            _redrawing = true;
            Dispatcher.UIThread.Post(Redraw, DispatcherPriority.Background);
        }
    }

    // Only the blocks that changed are drawn again; as an answer grows, that is just the last few.
    private void Redraw()
    {
        _redrawing = false;
        var blocks = Blocks;
        var kept = 0;

        while (kept < _drawn.Count && kept < blocks.Count && Same(_drawn[kept], blocks[kept]))
        {
            kept++;
        }

        _drawn.RemoveRange(kept, _drawn.Count - kept);
        _page.Children.RemoveRange(kept, _page.Children.Count - kept);

        foreach (var block in blocks.Skip(kept))
        {
            _drawn.Add(block);
            _page.Children.Add(Draw(block, group: null, depth: 0));
        }
    }

    private static bool Same(Md.Block one, Md.Block other) => (one, other) switch
    {
        (Md.Paragraph a, Md.Paragraph b) => a.Fragments.SequenceEqual(b.Fragments),
        (Md.Heading a, Md.Heading b) => a.Level == b.Level && a.Fragments.SequenceEqual(b.Fragments),
        (Md.ItemList a, Md.ItemList b) => a.Ordered == b.Ordered && a.Start == b.Start && a.Items.Count == b.Items.Count
            && a.Items.Zip(b.Items).All(pair => pair.First.Checked == pair.Second.Checked && Same(pair.First.Blocks, pair.Second.Blocks)),
        (Md.Quote a, Md.Quote b) => Same(a.Blocks, b.Blocks),
        (Md.Table a, Md.Table b) => a.Columns.SequenceEqual(b.Columns) && a.Rows.Count == b.Rows.Count
            && a.Rows.Zip(b.Rows).All(pair => pair.First.IsHeader == pair.Second.IsHeader && pair.First.Cells.Count == pair.Second.Cells.Count
                && pair.First.Cells.Zip(pair.Second.Cells).All(cell => cell.First.SequenceEqual(cell.Second))),
        _ => one.Equals(other),
    };

    private static bool Same(IReadOnlyList<Md.Block> one, IReadOnlyList<Md.Block> other) =>
        one.Count == other.Count && one.Zip(other).All(pair => Same(pair.First, pair.Second));

    private static StackPanel Stack(IReadOnlyList<Md.Block> blocks, object? group, int depth, double spacing)
    {
        var panel = new StackPanel { Spacing = spacing };

        foreach (var block in blocks)
        {
            panel.Children.Add(Draw(block, group, depth));
        }

        return panel;
    }

    private static Control Draw(Md.Block block, object? group, int depth) => block switch
    {
        Md.Heading heading => Text(heading.Fragments, group, heading.Level switch { 1 => "h1", 2 => "h2", _ => "h3" }),
        Md.Paragraph paragraph => Text(paragraph.Fragments, group, "paragraph"),
        Md.ItemList list => ListOf(list, group ?? list, depth),
        Md.Quote quote => Classed(new Border { Child = Stack(quote.Blocks, group, depth, BlockSpacing) }, "prose-quote"),
        Md.Code { Language: "markdown" or "md" } code => Drafted(code, group, depth),
        Md.Code code => Source(code, group),
        Md.Table table => TableOf(table),
        Md.Rule => Classed(new Rectangle(), "prose-rule"),
        _ => new Panel(),
    };

    private static Border Source(Md.Code code, object? group, bool copyable = true)
    {
        var text = Classed(new SelectableText { Group = group }, "prose", "code");
        var inlines = new InlineCollection();
        var lines = code.Text.TrimEnd('\n');

        foreach (var token in Highlighter.Tokens(lines, Grammars.For(code.Language ?? string.Empty)))
        {
            var run = new Run(token.Text);

            if (Shades.TryGetValue(token.Shade, out var brush))
            {
                run.Bind(TextElement.ForegroundProperty, text.GetResourceObservable(brush));
            }

            if (token.Shade == Shade.Comment)
            {
                run.FontStyle = FontStyle.Italic;
            }

            inlines.Add(run);
        }

        text.Inlines = inlines;

        var shown = new Panel { Children = { Sideways(text) } };

        if (copyable)
        {
            shown.Children.Add(Classed(new CopyButton { Text = lines }, "prose-copy"));
        }

        return Classed(new Border { Child = shown }, "prose-code");
    }

    // Markdown an agent writes out, like a summary or a README, reads drawn; its source is a click away.
    private static StackPanel Drafted(Md.Code code, object? group, int depth)
    {
        var drawn = Classed(new Border { Child = Stack(Md.Prose.Parse(code.Text), group, depth, BlockSpacing) }, "prose-preview");
        var source = Source(code, group, copyable: false);
        source.IsVisible = false;

        var preview = Classed(new Button { Content = "Preview" }, "chip", "chosen");
        var raw = Classed(new Button { Content = "Source" }, "chip");

        void Show(bool drawing)
        {
            drawn.IsVisible = drawing;
            source.IsVisible = !drawing;
            preview.Classes.Set("chosen", drawing);
            raw.Classes.Set("chosen", !drawing);
        }

        preview.Click += (_, _) => Show(drawing: true);
        raw.Click += (_, _) => Show(drawing: false);

        var switches = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right };
        switches.Children.Add(new CopyButton { Text = code.Text.TrimEnd('\n') });
        switches.Children.Add(preview);
        switches.Children.Add(raw);

        return new StackPanel { Spacing = 6, Children = { switches, drawn, source } };
    }

    private static Grid ListOf(Md.ItemList list, object group, int depth)
    {
        var grid = Classed(new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 8, RowSpacing = ItemSpacing }, "prose-list");

        for (var at = 0; at < list.Items.Count; at++)
        {
            var item = list.Items[at];
            var row = new object();
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var marker = Plain(MarkerOf(list, at, item, depth), group, "marker");
            marker.IsMarker = true;
            marker.Row = row;

            var content = Stack(item.Blocks, group, depth + 1, ItemSpacing);

            if (content.Children.Count > 0 && content.Children[0] is SelectableText first)
            {
                first.Row = row;
            }

            Grid.SetRow(marker, at);
            Grid.SetRow(content, at);
            Grid.SetColumn(content, 1);
            grid.Children.Add(marker);
            grid.Children.Add(content);
        }

        return grid;
    }

    private static string MarkerOf(Md.ItemList list, int at, Md.Item item, int depth) => item.Checked switch
    {
        true => "☑",
        false => "☐",
        null when list.Ordered => $"{list.Start + at}.",
        null => Bullets[depth % Bullets.Length],
    };

    private static Border TableOf(Md.Table table)
    {
        var grid = new Grid();

        foreach (var _ in table.Columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }

        for (var at = 0; at < table.Rows.Count; at++)
        {
            var source = table.Rows[at];
            var row = new object();
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            for (var column = 0; column < table.Columns.Count; column++)
            {
                var text = Text(column < source.Cells.Count ? source.Cells[column] : [], table, source.IsHeader ? "th" : "td");
                text.Row = row;
                text.TextAlignment = table.Columns[column] switch
                {
                    Md.Alignment.Center => TextAlignment.Center,
                    Md.Alignment.End => TextAlignment.Right,
                    _ => TextAlignment.Left,
                };

                var cell = Classed(new Border { Child = text }, "prose-cell");
                cell.Classes.Set("header", source.IsHeader);
                cell.Classes.Set("last", at == table.Rows.Count - 1);
                Grid.SetRow(cell, at);
                Grid.SetColumn(cell, column);
                grid.Children.Add(cell);
            }
        }

        return Classed(new Border { Child = Sideways(grid) }, "prose-table");
    }

    // Wide code and tables scroll sideways instead of squeezing the column.
    private static ScrollViewer Sideways(Control content) => new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Content = content,
    };

    private static SelectableText Plain(string text, object? group, string kind) =>
        Classed(new SelectableText { Group = group, Text = text }, "prose", kind);

    private static SelectableText Text(IReadOnlyList<Md.Fragment> fragments, object? group, string kind)
    {
        var block = Classed(new SelectableText { Group = group }, "prose", kind);
        var inlines = new InlineCollection();
        List<LinkSpan> links = [];
        var at = 0;

        foreach (var fragment in fragments)
        {
            var run = new Run(fragment.Text);
            Dress(run, fragment, block);

            if (fragment.Link is { } url)
            {
                links.Add(new LinkSpan(at, at + fragment.Text.Length, url));
            }

            inlines.Add(run);
            at += fragment.Text.Length;
        }

        block.Inlines = inlines;
        block.Links = links;

        return block;
    }

    private static void Dress(Run run, Md.Fragment fragment, SelectableText block)
    {
        if (fragment.Look.HasFlag(Md.Look.Bold))
        {
            run.FontWeight = FontWeight.SemiBold;
        }

        if (fragment.Look.HasFlag(Md.Look.Italic))
        {
            run.FontStyle = FontStyle.Italic;
        }

        if (fragment.Look.HasFlag(Md.Look.Strike))
        {
            run.TextDecorations = TextDecorations.Strikethrough;
        }

        if (fragment.Look.HasFlag(Md.Look.Code))
        {
            run.Bind(TextElement.FontFamilyProperty, block.GetResourceObservable("MonoFont"));
            run.Bind(TextElement.FontSizeProperty, block.GetResourceObservable("FontSizeBody"));
            run.Bind(TextElement.ForegroundProperty, block.GetResourceObservable("AccentLightBrush"));
            run.Bind(TextElement.BackgroundProperty, block.GetResourceObservable("SurfaceBrush"));
        }

        if (fragment.Link is not null)
        {
            run.Bind(TextElement.ForegroundProperty, block.GetResourceObservable("AccentTextBrush"));
            run.TextDecorations = TextDecorations.Underline;
        }
    }

    private static T Classed<T>(T element, params string[] classes)
        where T : StyledElement
    {
        element.Classes.AddRange(classes);

        return element;
    }
}
