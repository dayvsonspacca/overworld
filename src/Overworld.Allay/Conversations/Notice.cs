namespace Overworld.Allay.Conversations;

public sealed record Notice(NoticeKind Kind, string Text) : Moment;
