using Avalonia.Input;

namespace Overworld.Desktop.Commands;

public sealed record Shortcut
{
    private Shortcut(string keys, IReadOnlyList<KeyGesture> steps)
    {
        Keys = keys;
        Steps = steps;
    }

    public string Keys { get; }

    public IReadOnlyList<KeyGesture> Steps { get; }

    public static Shortcut Of(string keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keys);

        var steps = keys.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (steps.Length > 2)
        {
            throw new ArgumentException($"'{keys}' has {steps.Length} steps; a shortcut has one or two, like Ctrl+K Ctrl+O.", nameof(keys));
        }

        return new Shortcut(string.Join(' ', steps), [.. steps.Select(Gesture)]);
    }

    public bool Equals(Shortcut? other) => other is not null && Keys == other.Keys;

    public override int GetHashCode() => Keys.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Keys;

    private static KeyGesture Gesture(string step)
    {
        var plus = step.LastIndexOf('+', step.Length - 2 >= 0 ? step.Length - 2 : 0);
        var modifiers = plus > 0 ? step[..(plus + 1)] : string.Empty;
        var key = plus > 0 ? step[(plus + 1)..] : step;
        var named = key switch
        {
            "," => "OemComma",
            "/" => "OemQuestion",
            "." => "OemPeriod",
            "+" => "OemPlus",
            "-" => "OemMinus",
            [var digit] when char.IsAsciiDigit(digit) => $"D{digit}",
            _ => key,
        };

        return KeyGesture.Parse(modifiers + named);
    }
}
