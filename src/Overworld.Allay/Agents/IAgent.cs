using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Agents;

public interface IAgent
{
    AgentId Id { get; }

    string Name { get; }

    string Summary { get; }

    IReadOnlyList<string> InstructionFiles { get; }

    Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken);

    Task<Readiness> CheckAsync(CancellationToken cancellationToken);

    ISession Begin(Brief brief, string? resume = null);
}
