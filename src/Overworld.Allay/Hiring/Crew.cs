using Overworld.Allay.Agents;
using Overworld.Allay.Settings;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Hiring;

public sealed class Crew(IReadOnlyList<IAgent> roster, StaffFile staff)
{
    public IReadOnlyList<IAgent> Roster => roster;

    public async Task<Result<IReadOnlyList<AgentCard>>> CardsAsync(CancellationToken cancellationToken)
    {
        var read = await staff.ReadAsync(cancellationToken);

        return read.IsSuccess ? Result.Success<IReadOnlyList<AgentCard>>([.. roster.Select(agent => CardOf(agent, read.Value))]) : read.Error;
    }

    public async Task<Result<IReadOnlyList<IAgent>>> HiredAsync(CancellationToken cancellationToken)
    {
        var read = await staff.ReadAsync(cancellationToken);

        return read.IsSuccess
            ? Result.Success<IReadOnlyList<IAgent>>([.. roster.Where(agent => (read.Value.Hired ?? []).Contains(agent.Id.Value))])
            : read.Error;
    }

    public async Task<Result<Configuration>> ConfigurationOfAsync(string id, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        var read = await staff.ReadAsync(cancellationToken);

        return read.IsSuccess ? ConfigurationOf(agent.Value, read.Value) : read.Error;
    }

    public async Task<Result<Readiness>> CheckAsync(string id, CancellationToken cancellationToken)
    {
        var configuration = await ConfigurationOfAsync(id, cancellationToken);

        return configuration.IsSuccess ? await Find(id).Value.CheckAsync(configuration.Value, cancellationToken) : configuration.Error;
    }

    public async Task<Result<AgentCard>> HireAsync(string id, bool hired, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        if (!agent.IsSuccess)
        {
            return agent.Error;
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

        return written.IsSuccess ? CardOf(agent.Value, written.Value) : written.Error;
    }

    public async Task<Result<AgentCard>> ConfigureAsync(string id, string key, string? value, CancellationToken cancellationToken)
    {
        var agent = Find(id);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        var wanted = value?.Trim() is { Length: > 0 } trimmed ? trimmed : null;

        var written = await staff.UpdateAsync(
            saved =>
            {
                var current = ConfigurationOf(agent.Value, saved);

                if (agent.Value.Settings(current).FirstOrDefault(setting => setting.Key == key) is not { } setting)
                {
                    return new Error("agent.setting_unknown", $"{agent.Value.Name} has no setting '{key}'.");
                }

                if (setting is Choice choice && !choice.Alternatives.Any(alternative => alternative.Value == wanted))
                {
                    return new Error(
                        "agent.value_unknown",
                        $"'{value}' is not one of {setting.Title}'s values: {string.Join(", ", choice.Alternatives.Select(alternative => alternative.Value ?? "(default)"))}.");
                }

                var changed = current.With(key, wanted);

                if (changed.Equals(current))
                {
                    return saved;
                }

                var configurations = new Dictionary<string, IReadOnlyDictionary<string, string>>(saved.Configurations ?? new Dictionary<string, IReadOnlyDictionary<string, string>>());

                if (changed.Values.Count > 0)
                {
                    configurations[agent.Value.Id.Value] = changed.Values;
                }
                else
                {
                    configurations.Remove(agent.Value.Id.Value);
                }

                return saved with { Configurations = configurations.Count > 0 ? configurations : null };
            },
            cancellationToken);

        return written.IsSuccess ? CardOf(agent.Value, written.Value) : written.Error;
    }

    public Result<IAgent> Find(string id) =>
        roster.FirstOrDefault(agent => agent.Id.Value == id.Trim()) is { } agent
            ? Result.Success(agent)
            : new Error(
                "agent.unknown",
                roster.Count == 0
                    ? $"There is no agent '{id}'; Overworld knows no agents yet."
                    : $"There is no agent '{id}'; it is one of {string.Join(", ", roster.Select(each => each.Id.Value))}.");

    private static Configuration ConfigurationOf(IAgent agent, Staff staff) =>
        staff.Configurations?.GetValueOrDefault(agent.Id.Value) is { } values ? new Configuration(values) : Configuration.Empty;

    private static AgentCard CardOf(IAgent agent, Staff staff)
    {
        var configuration = ConfigurationOf(agent, staff);

        return new AgentCard(
            agent.Id.Value,
            agent.Name,
            agent.Summary,
            (staff.Hired ?? []).Contains(agent.Id.Value),
            agent.Settings(configuration),
            configuration.Values);
    }
}
