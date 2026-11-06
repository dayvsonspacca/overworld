
namespace Overworld.Apps.Crafter.Agents;

public sealed class Crew
{
    private readonly List<IAgent> _roster;

    private List<IAgent> _hired = [];

    public Crew(IEnumerable<IAgent> roster)
    {
        _roster = [.. roster];
        if (_roster.Count == 0)
        {
            throw new ArgumentException("A crew needs at least one agent to hire.", nameof(roster));
        }
    }

    public IReadOnlyList<IAgent> Roster => _roster;

    public IReadOnlyList<IAgent> Hired => _hired;

    public bool IsHiring => _hired.Count == 0;

    // Every instructions file an agent on the roster reads, the one shared by all of them first.
    public IReadOnlyList<string> InstructionFiles => [.. _roster.SelectMany(agent => agent.InstructionFiles).Prepend(AgentInstructions.Shared).Distinct()];

    public IAgent Default => _hired.Count > 0 ? _hired[0] : _roster[0];

    public void Hire(IEnumerable<AgentId> agents)
    {
        HashSet<AgentId> wanted = [.. agents];

        _hired = _roster.FindAll(agent => wanted.Contains(agent.Id));
    }

    public bool IsHired(AgentId id) => _hired.Exists(agent => agent.Id == id);

    public IAgent Find(AgentId id) => _roster.Find(agent => agent.Id == id) ?? Default;
}
