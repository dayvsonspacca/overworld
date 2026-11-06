using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

[Operation("agents-hire", "Hires an agent, so apps offer it, or lets it go.")]
public sealed class HireAgentOp(Crew crew) : IOperation<HireAgent, AgentCard>
{
    public Task<Result<AgentCard>> ExecuteAsync(HireAgent input, CancellationToken cancellationToken) =>
        crew.HireAsync(input.Agent, input.Hired, cancellationToken);
}
