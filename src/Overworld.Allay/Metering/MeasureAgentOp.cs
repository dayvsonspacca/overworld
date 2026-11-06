using Overworld.Allay.Hiring;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Metering;

[Operation("agents-usage", "Tells how much of its limits an agent has used, when each resets, and what it has spent.")]
public sealed class MeasureAgentOp(Crew crew) : IOperation<MeasureAgent, Usage>
{
    public Task<Result<Usage>> ExecuteAsync(MeasureAgent input, CancellationToken cancellationToken) =>
        crew.UsageAsync(input.Agent, cancellationToken);
}
