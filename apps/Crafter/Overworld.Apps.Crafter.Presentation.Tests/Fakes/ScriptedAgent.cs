using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Apps.Crafter.Presentation.Tests.Fakes;

public sealed class ScriptedAgent(ScriptedSession session) : IAgent
{
    public AgentId Id { get; } = new("scripted");

    public string Name => "Scripted";

    public string Summary => "Answers from a script.";

    public IReadOnlyList<string> InstructionFiles => [];

    public Task<IReadOnlyList<ModelOption>> ModelsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ModelOption>>([new ModelOption("fast", "Fast")]);

    public Task<Readiness> CheckAsync(CancellationToken cancellationToken) => Task.FromResult<Readiness>(new Ready("Ready", "Scripted"));

    public Brief? Brief { get; private set; }

    public ISession Begin(Brief brief, string? resume = null)
    {
        Brief = brief;

        return session;
    }
}
