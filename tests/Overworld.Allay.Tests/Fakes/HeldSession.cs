using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Tests.Fakes;

public sealed class HeldSession(Brief brief, string? resume) : ISession
{
    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke;

    public Channel<Moment> Feed { get; } = Channel.CreateUnbounded<Moment>();

    public Brief Brief { get; private set; } = brief;

    public string? Resumed { get; } = resume;

    public string? Id => "held";

    public IReadOnlyList<Job> Jobs => [];

    public List<Prompt> Heard { get; } = [];

    public List<(string Request, Decision Decision)> Decided { get; } = [];

    public bool Ended { get; private set; }

    public async IAsyncEnumerable<Moment> ReplyAsync(Prompt prompt, Setup setup, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Heard.Add(prompt);

        await foreach (var moment in Feed.Reader.ReadAllAsync(CancellationToken.None))
        {
            yield return moment;

            if (moment is Outcome)
            {
                yield break;
            }
        }
    }

    public void Wake(params Moment[] moments)
    {
        var woke = Channel.CreateUnbounded<Moment>();

        foreach (var moment in moments)
        {
            woke.Writer.TryWrite(moment);
        }

        woke.Writer.TryComplete();
        Woke?.Invoke(this, woke.Reader.ReadAllAsync());
    }

    public void Rebrief(Brief brief) => Brief = brief;

    public void Decide(string request, Decision decision) => Decided.Add((request, decision));

    public string? Output(string job) => $"output of {job}";

    public void Stop(string job) => JobsChanged?.Invoke(this, []);

    public void End() => Ended = true;
}
