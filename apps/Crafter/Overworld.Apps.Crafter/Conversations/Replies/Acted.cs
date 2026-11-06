namespace Overworld.Apps.Crafter.Conversations.Replies;

public sealed record Acted(string Id, bool Failed, string Output) : Reply;
