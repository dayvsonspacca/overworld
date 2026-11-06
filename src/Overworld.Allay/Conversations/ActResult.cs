namespace Overworld.Allay.Conversations;

public sealed record ActResult(string Id, bool Succeeded, string Output) : Moment;
