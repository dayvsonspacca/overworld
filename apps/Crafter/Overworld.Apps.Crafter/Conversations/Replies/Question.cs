namespace Overworld.Apps.Crafter.Conversations.Replies;

public sealed record Question(string Text, string Header, bool Several, IReadOnlyList<Pick> Picks);
