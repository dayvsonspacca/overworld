using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Tests.Fakes;

public sealed class HeldAgent(string id) : IAgent
{
    public AgentId Id { get; } = new(id);

    public string Name => $"Agent {id}";

    public string Summary => "Says what it is fed.";

    public string Color => "#D97757";

    public IReadOnlyList<string> InstructionFiles => [];

    public List<HeldSession> Sessions { get; } = [];

    public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ModelOption>>([]);

    public Task<Readiness> CheckAsync(CancellationToken cancellationToken) => Task.FromResult<Readiness>(new Ready("Ready", "1"));

    public ISession Begin(Brief brief, string? resume = null)
    {
        var session = new HeldSession(brief, resume);
        Sessions.Add(session);

        return session;
    }
}
