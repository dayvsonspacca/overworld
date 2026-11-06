using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Metering;
using Overworld.Allay.Settings;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Tests.Fakes;

public sealed class MeteredAgent(string id, Usage usage) : IAgent, IMetered
{
    private readonly ScriptedAgent _agent = new(id);

    public AgentId Id => _agent.Id;

    public string Name => _agent.Name;

    public string Summary => _agent.Summary;

    public string Color => "#D97757";

    public IReadOnlyList<string> InstructionFiles => _agent.InstructionFiles;

    public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) => _agent.ModelsAsync(cancellationToken);

    public Task<Readiness> CheckAsync(CancellationToken cancellationToken) => _agent.CheckAsync(cancellationToken);

    public ISession Begin(Brief brief, string? resume = null) => _agent.Begin(brief, resume);

    public Task<Result<Usage>> UsageAsync(CancellationToken cancellationToken) => Task.FromResult<Result<Usage>>(usage);
}
