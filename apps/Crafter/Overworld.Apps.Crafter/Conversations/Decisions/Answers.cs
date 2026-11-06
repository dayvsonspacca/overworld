namespace Overworld.Apps.Crafter.Conversations.Decisions;

public sealed record Answers(IReadOnlyList<Answer> Given) : Decision;
