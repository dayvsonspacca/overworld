using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Metering;
using Overworld.Allay.Settings;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Allay.Presentation.Tests.Fakes;

public sealed class MeteredAgent(string id, string name, Readiness readiness) : IAgent, IMetered
{
    private readonly FakeAgent _agent = new(id, name, readiness);

    public AgentId Id => _agent.Id;

    public string Name => _agent.Name;

    public string Summary => _agent.Summary;

    public string Color => "#D97757";

    public IReadOnlyList<string> InstructionFiles => _agent.InstructionFiles;

    public Readiness Readiness
    {
        get => _agent.Readiness;
        set => _agent.Readiness = value;
    }

    public Result<Usage> Usage { get; set; } = new Usage([]);

    public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) => _agent.ModelsAsync(cancellationToken);

    public Task<Readiness> CheckAsync(CancellationToken cancellationToken) => _agent.CheckAsync(cancellationToken);

    public ISession Begin(Brief brief, string? resume = null) => _agent.Begin(brief, resume);

    public Task<Result<Usage>> UsageAsync(CancellationToken cancellationToken) => Task.FromResult(Usage);
}
