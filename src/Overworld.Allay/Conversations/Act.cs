namespace Overworld.Allay.Conversations;

public sealed record Act(string Id, ActKind Kind, string Subject, Changes? Changes = null) : Moment;
