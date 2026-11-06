using Overworld.Allay.Conversations;

namespace Overworld.Allay.Agents;

public sealed record PastSession(string Id, string Title, DateTimeOffset LastActive, Prompt? LastPrompt = null);
