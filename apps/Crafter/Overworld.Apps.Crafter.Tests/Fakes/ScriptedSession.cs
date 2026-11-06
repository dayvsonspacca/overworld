using System.Runtime.CompilerServices;
using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Apps.Crafter.Tests.Fakes;

public sealed class ScriptedSession(params IReadOnlyList<Moment>[] replies) : ISession
{
    private int _next;

    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke;

    public string? Id { get; set; }

    public IReadOnlyList<Job> Jobs => [];

    public List<Brief> Briefs { get; } = [];

    public List<(string Request, Decision Decision)> Decisions { get; } = [];

    public async IAsyncEnumerable<Moment> ReplyAsync(Prompt prompt, Setup setup, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var moment in replies[_next++])
        {
            await Task.Yield();

            yield return moment;
        }
    }

    public void Rebrief(Brief brief) => Briefs.Add(brief);

    public void Decide(string request, Decision decision) => Decisions.Add((request, decision));

    public string? Output(string job) => null;

    public void Stop(string job) => JobsChanged?.Invoke(this, []);

    public void End() => Woke?.Invoke(this, AsyncEnumerable.Empty<Moment>());
}
