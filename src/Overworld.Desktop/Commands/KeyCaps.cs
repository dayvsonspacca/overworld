using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Overworld.Desktop.Commands;

public sealed class KeyCaps : StackPanel
{
    public static readonly StyledProperty<Shortcut?> ShortcutProperty =
        AvaloniaProperty.Register<KeyCaps, Shortcut?>(nameof(Shortcut));

    public KeyCaps()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 4;
    }

    public Shortcut? Shortcut
    {
        get => GetValue(ShortcutProperty);
        set => SetValue(ShortcutProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShortcutProperty)
        {
            Lay();
        }
    }

    private void Lay()
    {
        Children.Clear();

        if (Shortcut is null)
        {
            return;
        }

        var steps = Shortcut.Keys.Split(' ');

        for (var step = 0; step < steps.Length; step++)
        {
            if (step > 0)
            {
                Children.Add(new TextBlock { Text = "then", Classes = { "then" } });
            }

            foreach (var key in Keys(steps[step]))
            {
                Children.Add(new Border { Classes = { "key" }, Child = new TextBlock { Text = key } });
            }
        }
    }

    private static IEnumerable<string> Keys(string step)
    {
        var plus = step.LastIndexOf('+', Math.Max(0, step.Length - 2));

        return plus > 0 ? [.. step[..plus].Split('+'), step[(plus + 1)..]] : [step];
    }
}
