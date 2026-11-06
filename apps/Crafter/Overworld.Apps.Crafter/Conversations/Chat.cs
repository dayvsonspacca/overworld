using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Allay.Settings;

namespace Overworld.Apps.Crafter.Conversations;

public sealed class Chat(ChatId id, AgentId agent, ChatTitle title, Conversation conversation)
{
    public ChatId Id { get; } = id;

    public AgentId Agent { get; } = agent;

    public ChatTitle Title { get; private set; } = title;

    public Conversation Conversation { get; } = conversation;

    public ISession Session => Conversation.Session;

    public IReadOnlyList<Turn> Turns => Conversation.Turns;

    public IAsyncEnumerable<Moment> AskAsync(Prompt prompt, Setup setup, CancellationToken cancellationToken)
    {
        if (Conversation.Turns.Count == 0)
        {
            Title = ChatTitle.From(prompt);
        }

        return Conversation.AskAsync(prompt, setup, cancellationToken);
    }

    public ChatRecord Record() => new(Id, Agent, Title, Session.Id, [.. Turns]);
}
