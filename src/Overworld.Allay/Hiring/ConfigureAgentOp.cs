using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

[Operation("agents-configure", "Sets one of an agent's settings, for every app that runs it, or puts it back to its default.")]
public sealed class ConfigureAgentOp(Crew crew) : IOperation<ConfigureAgent, AgentCard>
{
    public Task<Result<AgentCard>> ExecuteAsync(ConfigureAgent input, CancellationToken cancellationToken) =>
        crew.ConfigureAsync(input.Agent, input.Key, input.Value, cancellationToken);
}
