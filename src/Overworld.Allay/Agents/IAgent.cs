using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Agents;

public interface IAgent
{
    AgentId Id { get; }

    string Name { get; }

    string Summary { get; }

    IReadOnlyList<string> InstructionFiles { get; }

    IReadOnlyList<Setting> Settings(Configuration configuration);

    Task<Readiness> CheckAsync(Configuration configuration, CancellationToken cancellationToken);

    ISession Begin(Brief brief, string? resume = null);
}
