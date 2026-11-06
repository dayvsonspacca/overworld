using Overworld.Apps.Crafter.Agents;
using Overworld.Apps.Crafter.Agents.Background;
using Overworld.Apps.Crafter.Agents.Options;
using Overworld.Apps.Crafter.Conversations.Decisions;
using Overworld.Apps.Crafter.Conversations.Replies;

namespace Overworld.Apps.Crafter.Conversations;

public sealed class Chat(ChatId id, AgentId agent, ChatTitle title, Conversation conversation)
{
    public ChatId Id { get; } = id;

    public AgentId Agent { get; } = agent;

    public ChatTitle Title { get; private set; } = title;

    public IReadOnlyList<Turn> Turns => conversation.Turns;

    public string? SessionId => conversation.SessionId;

    public event EventHandler<IReadOnlyList<Job>>? JobsChanged
    {
        add => conversation.JobsChanged += value;
        remove => conversation.JobsChanged -= value;
    }

    public event EventHandler<IAsyncEnumerable<Reply>>? Woke
    {
        add => conversation.Woke += value;
        remove => conversation.Woke -= value;
    }

    public IReadOnlyList<Job> Jobs => conversation.Jobs;

    public void Decide(string request, Decision decision) => conversation.Decide(request, decision);

    public string? Output(string job) => conversation.Output(job);

    public void Stop(string job) => conversation.Stop(job);

    public void Dismiss() => conversation.Dismiss();

    public bool Runs(IAgentSession session) => conversation.Runs(session);

    public IAsyncEnumerable<Reply> Ask(Prompt prompt, Configuration preferred, CancellationToken cancellationToken = default)
    {
        if (conversation.Turns.Count == 0)
        {
            Title = ChatTitle.From(prompt);
        }

        return conversation.Ask(prompt, preferred, cancellationToken);
    }

    public ChatRecord Record() => new(Id, Agent, Title, conversation.SessionId, [.. conversation.Turns]);
}
