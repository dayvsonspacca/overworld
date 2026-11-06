namespace Overworld.Desktop.Commands;

public sealed class CommandBook
{
    public const string Everywhere = "Everywhere";

    private static readonly Command[] Basics =
    [
        new("Copy the selection", Everywhere, Keys: Shortcut.Of("Ctrl+C")),
        new("Select all", Everywhere, Keys: Shortcut.Of("Ctrl+A")),
        new("Undo / redo typing", Everywhere, Keys: Shortcut.Of("Ctrl+Z")),
        new("Close a dialog or a menu", Everywhere, Keys: Shortcut.Of("Escape")),
        new("Go to the start of a page", Everywhere, Keys: Shortcut.Of("Ctrl+Home")),
        new("Go to the end of a page", Everywhere, Keys: Shortcut.Of("Ctrl+End")),
        new("Show the shortcuts", Everywhere, Keys: Shortcut.Of("Ctrl+/")),
        new("Show every command", Everywhere, Keys: Shortcut.Of("Ctrl+Shift+P")),
    ];

    private readonly List<Command> _commands = [];

    public IReadOnlyList<Command> Own => _commands;

    public IReadOnlyList<Command> All => [.. _commands, .. Basics];

    public Command Add(string title, string group, Action run, string? keys = null)
    {
        var command = new Command(title, group, run, keys is null ? null : Shortcut.Of(keys));
        _commands.Add(command);

        return command;
    }

    public void Remove(Command command) => _commands.Remove(command);
}
