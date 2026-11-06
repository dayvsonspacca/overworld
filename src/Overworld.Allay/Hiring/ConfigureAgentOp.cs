using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

[Operation("agents-configure", "Sets an agent's mode, model or effort, for every app that runs it.")]
public sealed class ConfigureAgentOp(Crew crew) : IOperation<ConfigureAgent, AgentCard>
{
    public Task<Result<AgentCard>> ExecuteAsync(ConfigureAgent input, CancellationToken cancellationToken) =>
        crew.SetUpAsync(input.Agent, input.Mode, input.Model, input.Effort, cancellationToken);
}
