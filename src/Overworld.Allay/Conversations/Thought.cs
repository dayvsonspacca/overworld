namespace Overworld.Allay.Conversations;

public sealed record Thought(string Text, TimeSpan? Took = null) : Moment;
