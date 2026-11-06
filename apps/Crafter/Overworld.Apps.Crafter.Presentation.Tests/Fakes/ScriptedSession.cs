using System.Runtime.CompilerServices;
using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Apps.Crafter.Presentation.Tests.Fakes;

public sealed class ScriptedSession : ISession
{
    private readonly Queue<IReadOnlyList<Moment>> _replies = new();

    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke;

    public string? Id { get; set; } = "s-1";

    public IReadOnlyList<Job> Jobs { get; private set; } = [];

    public List<string> Stopped { get; } = [];

    public void Report(params Job[] jobs)
    {
        Jobs = jobs;
        JobsChanged?.Invoke(this, jobs);
    }

    public TaskCompletionSource? Held { get; set; }

    public List<Prompt> Heard { get; } = [];

    public List<Setup> Setups { get; } = [];

    public List<Brief> Briefs { get; } = [];

    public List<(string Request, Decision Decision)> Decisions { get; } = [];

    public bool Ended { get; private set; }

    public void Script(params Moment[] reply) => _replies.Enqueue(reply);

    public async IAsyncEnumerable<Moment> ReplyAsync(Prompt prompt, Setup setup, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Heard.Add(prompt);
        Setups.Add(setup);

        foreach (var moment in _replies.Dequeue())
        {
            if (moment is Outcome && Held is { } held)
            {
                Held = null;

                try
                {
                    await held.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    yield return new Stopped();

                    yield break;
                }
            }

            await Task.Yield();

            yield return moment;
        }
    }

    public void Wake(params Moment[] moments) => Woke?.Invoke(this, moments.ToAsyncEnumerable());

    public void Rebrief(Brief brief) => Briefs.Add(brief);

    public void Decide(string request, Decision decision) => Decisions.Add((request, decision));

    public string? Output(string job) => $"output of {job}";

    public void Stop(string job) => Stopped.Add(job);

    public void End() => Ended = true;
}
