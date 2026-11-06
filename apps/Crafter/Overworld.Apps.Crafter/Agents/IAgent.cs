using Overworld.Apps.Crafter.Agents.Options;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Agents;

public interface IAgent
{
    AgentId Id { get; }

    string Name { get; }

    string Summary { get; }

    // The instructions files this agent reads on its own in a project, which Crafter turns off.
    IReadOnlyList<string> InstructionFiles { get; }

    IReadOnlyList<Setting> Settings(Configuration configuration);

    Task<Readiness> Check(Configuration configuration, CancellationToken cancellationToken = default);

    IAgentSession Begin(Project project, string? resumeFrom = null);
}
