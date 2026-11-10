using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Remote;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Visits;

public sealed class Lobby(Crew crew, AllayLink? link = null)
{
    public async Task<Result<IAgent>> AgentAsync(CancellationToken cancellationToken)
    {
        var hired = await crew.HiredAsync(cancellationToken);

        if (!hired.IsSuccess)
        {
            return hired.Error;
        }

        return hired.Value is [var agent, ..]
            ? Result.Success(agent)
            : new Error("allay.no_agent", "No agent is hired yet. Hire one in Allay, or with: overworld:allay agents hire --agent claude-code");
    }

    public async Task<Result<Visit>> OpenAsync(Brief brief, CancellationToken cancellationToken)
    {
        var found = await AgentAsync(cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var agent = found.Value;
        var setup = await crew.SetupOfAsync(agent.Id.Value, cancellationToken);

        if (!setup.IsSuccess)
        {
            return setup.Error;
        }

        var session = await BeginAsync(agent, brief, cancellationToken);

        return new Visit(agent.Name, agent.Color, setup.Value, new Conversation(session, []));
    }

    private async Task<ISession> BeginAsync(IAgent agent, Brief brief, CancellationToken cancellationToken)
    {
        if (link is { } served && served.IsServing())
        {
            var opened = await served.OpenAsync(Guid.NewGuid().ToString(), agent.Id.Value, brief, null, [], keep: false, cancellationToken);

            if (opened.IsSuccess)
            {
                return opened.Value.Session;
            }
        }

        return agent.Begin(brief);
    }
}
