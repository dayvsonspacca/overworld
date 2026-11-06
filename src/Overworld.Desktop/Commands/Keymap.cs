using Avalonia.Input;

namespace Overworld.Desktop.Commands;

public sealed class Keymap(CommandBook book)
{
    private Command[] _waiting = [];

    public bool Handle(KeyEventArgs pressed)
    {
        if (IsModifier(pressed.Key))
        {
            return false;
        }

        var bound = book.Own.Where(command => command is { Run: not null, Keys: not null }).ToList();

        if (_waiting.Length > 0)
        {
            var started = _waiting;
            _waiting = [];

            return Run(started.Where(command => command.Keys!.Steps[1].Matches(pressed)));
        }

        Command[] starting = [.. bound.Where(command => command.Keys!.Steps.Count == 2 && command.Keys.Steps[0].Matches(pressed))];

        if (starting.Length > 0)
        {
            _waiting = starting;

            return true;
        }

        return Run(bound.Where(command => command.Keys!.Steps.Count == 1 && command.Keys.Steps[0].Matches(pressed)));
    }

    private static bool Run(IEnumerable<Command> matching)
    {
        foreach (var command in matching.Take(1))
        {
            command.Run!();

            return true;
        }

        return false;
    }

    private static bool IsModifier(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin;
}
