using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

[Operation("agents-hire", "Hires an agent that is ready to work, so apps offer it, or lets it go, and signals the apps.")]
public sealed class HireAgentOp(Crew crew, BeaconClient beacon) : IOperation<HireAgent, AgentCard>
{
    public async Task<Result<AgentCard>> ExecuteAsync(HireAgent input, CancellationToken cancellationToken)
    {
        var hired = await crew.HireAsync(input.Agent, input.Hired, cancellationToken);

        if (hired.IsSuccess)
        {
            await beacon.LightAsync(Crew.Signal, cancellationToken);
        }

        return hired;
    }
}
