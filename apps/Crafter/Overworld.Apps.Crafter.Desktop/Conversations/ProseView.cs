using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
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

    public IReadOnlyList<Md.Block> Blocks
    {
        get => GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BlocksProperty)
        {
            Child = Stack(Blocks, group: null, depth: 0, BlockSpacing);
        }
    }

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

    private static Border Source(Md.Code code, object? group)
    {
        var text = Classed(new SelectableText { Group = group }, "prose", "code");
        var inlines = new InlineCollection();

        foreach (var token in Highlighter.Tokens(code.Text.TrimEnd('\n'), Grammars.For(code.Language ?? string.Empty)))
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

        return Classed(new Border { Child = Sideways(text) }, "prose-code");
    }

    // Markdown an agent writes out, like a summary or a README, reads drawn; its source is a click away.
    private static StackPanel Drafted(Md.Code code, object? group, int depth)
    {
        var drawn = Classed(new Border { Child = Stack(Md.Prose.Parse(code.Text), group, depth, BlockSpacing) }, "prose-preview");
        var source = Source(code, group);
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
