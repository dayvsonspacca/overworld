namespace Overworld.Apps.Crafter.Conversations.Replies;

public sealed record NeedsAnswers(string Id, IReadOnlyList<Question> Questions) : Reply;
