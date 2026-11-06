using Overworld.Apps.Crafter.Conversations;

namespace Overworld.Apps.Crafter.Agents;

public sealed record PastChat(string SessionId, ChatTitle Title, DateTimeOffset LastActive, Prompt? LastPrompt = null);
