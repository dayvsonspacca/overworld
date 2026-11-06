using Overworld.Apps.Crafter.Agents;

namespace Overworld.Apps.Crafter.Conversations;

public sealed record ChatRecord(ChatId Id, AgentId Agent, ChatTitle Title, string? SessionId, IReadOnlyList<Turn> Turns);
