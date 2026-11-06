using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Apps.Allay.Presentation.Tests.Fakes;

public sealed class FakeAgent(string id, string name, Readiness readiness) : IAgent
{
    public AgentId Id { get; } = new(id);

    public string Name => name;

    public string Summary => $"{name} does what it is told.";

    public IReadOnlyList<string> InstructionFiles => [];

    public Readiness Readiness { get; set; } = readiness;

    public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ModelOption>>([new ModelOption("big", "Big"), new ModelOption("small", "Small")]);

    public Task<Readiness> CheckAsync(CancellationToken cancellationToken) => Task.FromResult(Readiness);

    public ISession Begin(Brief brief, string? resume = null) => throw new NotSupportedException();
}
