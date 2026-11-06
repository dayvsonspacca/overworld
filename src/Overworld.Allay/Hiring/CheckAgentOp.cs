using Overworld.Allay.Agents;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

[Operation("agents-check", "Checks whether an agent is ready to work on this machine and, when it is not, what to do about it.")]
public sealed class CheckAgentOp(Crew crew) : IOperation<CheckAgent, Readiness>
{
    public Task<Result<Readiness>> ExecuteAsync(CheckAgent input, CancellationToken cancellationToken) =>
        crew.CheckAsync(input.Agent, cancellationToken);
}
