using Overworld.Allay.Agents;
using Overworld.Allay.Metering;
using Overworld.Allay.Settings;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

public sealed class Crew(IReadOnlyList<IAgent> roster, StaffFile staff)
{
    public const string DefaultModel = "default";

    public const string Signal = "agents";

    public IReadOnlyList<IAgent> Roster => roster;

    public async Task<Result<IReadOnlyList<AgentCard>>> CardsAsync(CancellationToken cancellationToken)
    {
        var read = await staff.ReadAsync(cancellationToken);

        if (!read.IsSuccess)
        {
            return read.Error;
        }

        List<AgentCard> cards = [];

        foreach (var agent in roster)
        {
            cards.Add(await CardOfAsync(agent, read.Value, cancellationToken));
        }

        return Result.Success<IReadOnlyList<AgentCard>>(cards);
    }

    public async Task<Result<IReadOnlyList<IAgent>>> HiredAsync(CancellationToken cancellationToken)
    {
        var read = await staff.ReadAsync(cancellationToken);

        return read.IsSuccess
            ? Result.Success<IReadOnlyList<IAgent>>([.. roster.Where(agent => (read.Value.Hired ?? []).Contains(agent.Id.Value))])
            : read.Error;
    }

    public async Task<Result<Setup>> SetupOfAsync(string id, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        var read = await staff.ReadAsync(cancellationToken);

        return read.IsSuccess ? SetupOf(agent.Value, read.Value) : read.Error;
    }

    public async Task<Result<Readiness>> CheckAsync(string id, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        return agent.IsSuccess ? await agent.Value.CheckAsync(cancellationToken) : agent.Error;
    }

    public async Task<Result<Usage>> UsageAsync(string id, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        return agent.Value is IMetered metered
            ? await metered.UsageAsync(cancellationToken)
            : new Error("agent.unmetered", $"{agent.Value.Name} doesn't tell its usage.");
    }

    public async Task<Result<AgentCard>> HireAsync(string id, bool hired, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        if (hired && await agent.Value.CheckAsync(cancellationToken) is Blocked blocked)
        {
            return Unready(agent.Value, blocked);
        }

        var written = await staff.UpdateAsync(
            saved =>
            {
                var current = saved.Hired ?? [];

                if (current.Contains(agent.Value.Id.Value) == hired)
                {
                    return saved;
                }

                IReadOnlyList<string> changed = hired ? [.. current, agent.Value.Id.Value] : [.. current.Where(each => each != agent.Value.Id.Value)];

                return saved with { Hired = changed.Count > 0 ? changed : null };
            },
            cancellationToken);

        return written.IsSuccess ? await CardOfAsync(agent.Value, written.Value, cancellationToken) : written.Error;
    }

    public async Task<Result<AgentCard>> SetUpAsync(string id, Mode? mode, string? model, Effort? effort, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        var chosen = model?.Trim() is { Length: > 0 } trimmed ? trimmed : null;

        if (chosen is not null && chosen != DefaultModel)
        {
            var offered = await agent.Value.ModelsAsync(cancellationToken);

            if (offered.Count > 0 && !offered.Any(option => option.Id == chosen))
            {
                return new Error(
                    "agent.model_unknown",
                    $"{agent.Value.Name} offers no model '{chosen}'; it offers {string.Join(", ", offered.Select(option => option.Id))}, or {DefaultModel}.");
            }
        }

        var written = await staff.UpdateAsync(
            saved =>
            {
                var current = SetupOf(agent.Value, saved);
                var changed = current with
                {
                    Mode = mode ?? current.Mode,
                    Model = chosen is null ? current.Model : chosen == DefaultModel ? null : chosen,
                    Effort = effort ?? current.Effort,
                };

                if (changed == current)
                {
                    return saved;
                }

                var setups = new Dictionary<string, Setup>(saved.Setups ?? new Dictionary<string, Setup>());

                if (changed == Setup.Default)
                {
                    setups.Remove(agent.Value.Id.Value);
                }
                else
                {
                    setups[agent.Value.Id.Value] = changed;
                }

                return saved with { Setups = setups.Count > 0 ? setups : null };
            },
            cancellationToken);

        return written.IsSuccess ? await CardOfAsync(agent.Value, written.Value, cancellationToken) : written.Error;
    }

    public Result<IAgent> Find(string id) =>
        roster.FirstOrDefault(agent => agent.Id.Value == id.Trim()) is { } agent
            ? Result.Success(agent)
            : new Error(
                "agent.unknown",
                roster.Count == 0
                    ? $"There is no agent '{id}'; Overworld knows no agents yet."
                    : $"There is no agent '{id}'; it is one of {string.Join(", ", roster.Select(each => each.Id.Value))}.");

    private static Error Unready(IAgent agent, Blocked blocked) =>
        new(
            "agent.not_ready",
            $"{agent.Name} can only be hired once it is ready; it is {blocked.State.ToLowerInvariant()}: {blocked.Detail}"
            + (blocked.Remedy is { } remedy ? $" Run `{remedy}`." : string.Empty));

    private static Setup SetupOf(IAgent agent, Staff staff) =>
        staff.Setups?.GetValueOrDefault(agent.Id.Value) ?? Setup.Default;

    private static async Task<AgentCard> CardOfAsync(IAgent agent, Staff staff, CancellationToken cancellationToken) =>
        new(
            agent.Id.Value,
            agent.Name,
            agent.Summary,
            agent.Color,
            (staff.Hired ?? []).Contains(agent.Id.Value),
            agent is IMetered,
            SetupOf(agent, staff),
            await agent.ModelsAsync(cancellationToken));
}
