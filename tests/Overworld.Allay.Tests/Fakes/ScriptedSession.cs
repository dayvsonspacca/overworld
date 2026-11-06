using System.Runtime.CompilerServices;
using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Tests.Fakes;

public sealed class ScriptedSession(params IReadOnlyList<Moment>[] replies) : ISession
{
    private int _next;

    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke;

    public string? Id => "scripted";

    public IReadOnlyList<Job> Jobs => [];

    public List<Prompt> Heard { get; } = [];

    public List<Brief> Briefs { get; } = [];

    public async IAsyncEnumerable<Moment> ReplyAsync(
        Prompt prompt,
        Configuration configuration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Heard.Add(prompt);

        foreach (var moment in replies[_next++])
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            yield return moment;
        }
    }

    public void Wake(IReadOnlyList<Moment> moments) => Woke?.Invoke(this, Stream(moments));

    public void Rebrief(Brief brief) => Briefs.Add(brief);

    public void Decide(string request, Decision decision)
    {
    }

    public string? Output(string job) => null;

    public void Stop(string job) => JobsChanged?.Invoke(this, []);

    private static async IAsyncEnumerable<Moment> Stream(IReadOnlyList<Moment> moments)
    {
        foreach (var moment in moments)
        {
            await Task.Yield();

            yield return moment;
        }
    }
}
