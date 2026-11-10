using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Conversations;

public sealed record ChatRecord(ChatId Id, AgentId Agent, ChatTitle Title, string? SessionId, IReadOnlyList<Turn> Turns, DateTimeOffset? Touched = null)
{
    public bool Equals(ChatRecord? other) =>
        other is not null && Id == other.Id && Agent == other.Agent && Title == other.Title && SessionId == other.SessionId
        && Touched == other.Touched && Turns.SequenceEqual(other.Turns);

    public override int GetHashCode() => HashCode.Combine(Id, Agent, Title, SessionId, Turns.Count);
}
