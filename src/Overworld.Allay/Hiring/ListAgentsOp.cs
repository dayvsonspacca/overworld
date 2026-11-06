using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

[Operation("agents-list", "Lists the agents Overworld knows, whether each is hired, what can be set on it and how it is set.")]
public sealed class ListAgentsOp(Crew crew) : IOperation<ListAgents, IReadOnlyList<AgentCard>>
{
    public Task<Result<IReadOnlyList<AgentCard>>> ExecuteAsync(ListAgents input, CancellationToken cancellationToken) =>
        crew.CardsAsync(cancellationToken);
}
