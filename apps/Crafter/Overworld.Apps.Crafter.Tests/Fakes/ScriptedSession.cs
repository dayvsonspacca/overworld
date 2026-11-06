using System.Runtime.CompilerServices;
using Overworld.Apps.Crafter.Agents;
using Overworld.Apps.Crafter.Agents.Background;
using Overworld.Apps.Crafter.Agents.Options;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Conversations.Decisions;
using Overworld.Apps.Crafter.Conversations.Replies;

namespace Overworld.Apps.Crafter.Tests.Fakes;

public sealed class ScriptedSession(params Reply[] replies) : IAgentSession, IWorksInBackground
{
    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Reply>>? Woke;

    public IReadOnlyList<Job> Jobs { get; private set; } = [];

    public List<string> StoppedJobs { get; } = [];

    public bool IsDismissed { get; private set; }

    public Dictionary<string, string> Printed { get; } = [];

    public string? Output(string job) => Printed.GetValueOrDefault(job);

    public void Stop(string job) => StoppedJobs.Add(job);

    public void Dismiss() => IsDismissed = true;

    public void Run(params Job[] jobs)
    {
        Jobs = jobs;
        JobsChanged?.Invoke(this, jobs);
    }

    public void Wake(params Reply[] said) => Woke?.Invoke(this, said.ToAsyncEnumerable());

    public List<Prompt> Heard { get; } = [];

    public string? SessionId { get; set; }

    public List<Configuration> Configurations { get; } = [];

    public List<(string Request, Decision Decision)> Decisions { get; } = [];

    public bool Hangs { get; init; }

    public void Decide(string request, Decision decision) => Decisions.Add((request, decision));

    public async IAsyncEnumerable<Reply> Answer(Prompt prompt, Configuration configuration, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Heard.Add(prompt);
        Configurations.Add(configuration);

        foreach (var reply in replies)
        {
            await Task.Yield();

            yield return reply;
        }

        if (!Hangs)
        {
            yield break;
        }

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }

        yield return new Stopped();
    }
}
