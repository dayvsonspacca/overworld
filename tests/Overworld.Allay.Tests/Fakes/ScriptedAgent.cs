using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Tests.Fakes;

public sealed class ScriptedAgent(string id, Readiness? readiness = null) : IAgent
{
    public AgentId Id { get; } = new(id);

    public string Name => $"Agent {id}";

    public string Summary => "Does what it is told.";

    public IReadOnlyList<string> InstructionFiles => ["AGENT.md"];

    public Configuration? CheckedWith { get; private set; }

    public IReadOnlyList<Setting> Settings(Configuration configuration) =>
    [
        new Choice("model", "Model", "The model that answers.", [new(null, "Default"), new("big", "Big"), new("small", "Small")]),
        new Entry("executable", "Executable", "The command that starts it.", id),
    ];

    public Task<Readiness> CheckAsync(Configuration configuration, CancellationToken cancellationToken)
    {
        CheckedWith = configuration;

        return Task.FromResult(readiness ?? new Ready("Ready", "Version 1"));
    }

    public ISession Begin(Brief brief, string? resume = null) => new ScriptedSession();
}
