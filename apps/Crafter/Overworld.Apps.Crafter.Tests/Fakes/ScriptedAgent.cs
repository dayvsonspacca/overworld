using Overworld.Apps.Crafter.Agents;
using Overworld.Apps.Crafter.Agents.Options;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Conversations.Replies;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Tests.Fakes;

public sealed class ScriptedAgent(string id = "stand-in") : IAgent, IKeepsHistory
{
    public AgentId Id { get; } = new(id);

    public string Name => id;

    public string Summary => $"Stands in for {id}.";

    public IReadOnlyList<string> InstructionFiles { get; init; } = [];

    public Func<IAgentSession> Sessions { get; init; } = () => new ScriptedSession(new Finished());

    public Readiness Readiness { get; init; } = new Ready("Ready", "1.0");

    public IReadOnlyList<Turn> Replays { get; init; } = [];

    public IReadOnlyList<Setting> Settings(Configuration configuration) => [];

    public Task<Readiness> Check(Configuration configuration, CancellationToken cancellationToken = default) => Task.FromResult(Readiness);

    public IAgentSession Begin(Project project, string? resumeFrom = null) => Sessions();

    Task<Recollection> IKeepsHistory.History(Project project, int latest, CancellationToken cancellationToken) => Task.FromResult(new Recollection([], 0));

    public Task<IReadOnlyList<Turn>> Replay(Project project, string sessionId, CancellationToken cancellationToken = default) => Task.FromResult(Replays);
}
