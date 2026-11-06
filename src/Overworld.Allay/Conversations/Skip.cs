namespace Overworld.Allay.Conversations;

public sealed record Skip(string? Reason = null) : Decision;
