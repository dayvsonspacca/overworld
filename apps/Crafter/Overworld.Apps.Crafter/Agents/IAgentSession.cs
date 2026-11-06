using Overworld.Apps.Crafter.Agents.Options;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Conversations.Decisions;
using Overworld.Apps.Crafter.Conversations.Replies;

namespace Overworld.Apps.Crafter.Agents;

public interface IAgentSession
{
    string? SessionId { get; }

    void Decide(string request, Decision decision);

    IAsyncEnumerable<Reply> Answer(Prompt prompt, Configuration configuration, CancellationToken cancellationToken = default);
}
