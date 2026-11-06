using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Overworld.Desktop.Commands;

public sealed class HighlightedText : TextBlock
{
    public static readonly StyledProperty<string?> WordsProperty =
        AvaloniaProperty.Register<HighlightedText, string?>(nameof(Words));

    public static readonly StyledProperty<IReadOnlyList<int>?> HitsProperty =
        AvaloniaProperty.Register<HighlightedText, IReadOnlyList<int>?>(nameof(Hits));

    public static readonly StyledProperty<IBrush?> HitBrushProperty =
        AvaloniaProperty.Register<HighlightedText, IBrush?>(nameof(HitBrush));

    public string? Words
    {
        get => GetValue(WordsProperty);
        set => SetValue(WordsProperty, value);
    }

    public IReadOnlyList<int>? Hits
    {
        get => GetValue(HitsProperty);
        set => SetValue(HitsProperty, value);
    }

    public IBrush? HitBrush
    {
        get => GetValue(HitBrushProperty);
        set => SetValue(HitBrushProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WordsProperty || change.Property == HitsProperty || change.Property == HitBrushProperty)
        {
            Lay();
        }
    }

    private void Lay()
    {
        var words = Words ?? string.Empty;
        var hits = new HashSet<int>(Hits ?? []);
        var lines = new InlineCollection();
        var at = 0;

        while (at < words.Length)
        {
            var hit = hits.Contains(at);
            var end = at;

            while (end < words.Length && hits.Contains(end) == hit)
            {
                end++;
            }

            var run = new Run(words[at..end]);

            if (hit)
            {
                run.FontWeight = FontWeight.SemiBold;
                run.Foreground = HitBrush;
            }

            lines.Add(run);
            at = end;
        }

        Inlines = lines;
    }
}
