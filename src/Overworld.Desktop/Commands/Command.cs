namespace Overworld.Desktop.Commands;

public sealed record Command(string Title, string Group, Action? Run = null, Shortcut? Keys = null)
{
    public bool Runs => Run is not null;
}
