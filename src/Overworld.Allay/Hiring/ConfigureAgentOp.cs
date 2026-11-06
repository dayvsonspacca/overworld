using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

[Operation("agents-configure", "Sets an agent's mode, model or effort, for every app that runs it, and signals the apps.")]
public sealed class ConfigureAgentOp(Crew crew, BeaconClient beacon) : IOperation<ConfigureAgent, AgentCard>
{
    public async Task<Result<AgentCard>> ExecuteAsync(ConfigureAgent input, CancellationToken cancellationToken)
    {
        var configured = await crew.SetUpAsync(input.Agent, input.Mode, input.Model, input.Effort, cancellationToken);

        if (configured.IsSuccess)
        {
            await beacon.LightAsync(Crew.Signal, cancellationToken);
        }

        return configured;
    }
}
