using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Tests.Fakes;

public sealed class ScriptedAgent(string id, Readiness? readiness = null, IReadOnlyList<ModelOption>? models = null) : IAgent
{
    public AgentId Id { get; } = new(id);

    public string Name => $"Agent {id}";

    public string Summary => "Does what it is told.";

    public string Color => "#5FD3F3";

    public IReadOnlyList<string> InstructionFiles => ["AGENT.md"];

    public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(models ?? [new ModelOption("big", "Big"), new ModelOption("small", "Small")]);

    public Task<Readiness> CheckAsync(CancellationToken cancellationToken) =>
        Task.FromResult(readiness ?? new Ready("Ready", "Version 1"));

    public ISession Begin(Brief brief, string? resume = null) => new ScriptedSession();
}
