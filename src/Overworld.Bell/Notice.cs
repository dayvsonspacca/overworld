namespace Overworld.Bell;

public sealed record Notice(
    string Id,
    string From,
    string Title,
    string? Body,
    string? Context,
    string? Color,
    Ask? Ask,
    bool Quiet,
    DateTimeOffset Rung,
    Reply? Reply = null,
    bool Opened = false)
{
    public bool IsWaiting => Reply is null;
}
