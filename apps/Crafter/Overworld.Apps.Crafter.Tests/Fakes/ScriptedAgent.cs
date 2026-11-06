using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Apps.Crafter.Tests.Fakes;

public sealed class ScriptedAgent(string id, ScriptedSession session) : IAgent
{
    public AgentId Id { get; } = new(id);

    public string Name => "Scripted";

    public string Summary => "Answers from a script.";

    public IReadOnlyList<string> InstructionFiles => [];

    public List<(Brief Brief, string? Resume)> Begun { get; } = [];

    public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ModelOption>>([new ModelOption("fast", "Fast")]);

    public Task<Readiness> CheckAsync(CancellationToken cancellationToken) => Task.FromResult<Readiness>(new Ready("Ready", "Scripted"));

    public ISession Begin(Brief brief, string? resume = null)
    {
        Begun.Add((brief, resume));

        return session;
    }
}
