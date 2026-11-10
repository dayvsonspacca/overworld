using System.Runtime.CompilerServices;
using System.Text;
using Overworld.Allay.Agents;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Conversations;

public sealed class Conversation
{
    public const int LongestOutput = 4000;

    private readonly List<Turn> _turns;

    private readonly TimeProvider _clock;

    private readonly List<IAsyncEnumerable<Moment>> _held = [];

    private EventHandler<IAsyncEnumerable<Moment>>? _woke;

    public Conversation(ISession session, IEnumerable<Turn> history, TimeProvider? clock = null)
    {
        Session = session;
        _turns = [.. history];
        _clock = clock ?? TimeProvider.System;
        session.Woke += (_, moments) => Wake(Recorded(moments));
    }

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke
    {
        add
        {
            IAsyncEnumerable<Moment>[] held;

            lock (_held)
            {
                _woke += value;
                held = [.. _held];
                _held.Clear();
            }

            foreach (var moments in held)
            {
                value?.Invoke(this, moments);
            }
        }

        remove
        {
            lock (_held)
            {
                _woke -= value;
            }
        }
    }

    public ISession Session { get; }

    public IReadOnlyList<Turn> Turns => _turns;

    public IAsyncEnumerable<Moment> AskAsync(Prompt prompt, Setup setup, CancellationToken cancellationToken)
    {
        _turns.Add(prompt);

        return Recorded(Session.ReplyAsync(prompt, setup, cancellationToken), cancellationToken);
    }

    private void Wake(IAsyncEnumerable<Moment> moments)
    {
        EventHandler<IAsyncEnumerable<Moment>>? woke;

        lock (_held)
        {
            woke = _woke;

            if (woke is null)
            {
                _held.Add(moments);

                return;
            }
        }

        woke(this, moments);
    }

    public void Withdraw(Prompt prompt)
    {
        var at = _turns.LastIndexOf(prompt);

        if (at >= 0)
        {
            _turns.RemoveRange(at, _turns.Count - at);
        }
    }

    private async IAsyncEnumerable<Moment> Recorded(
        IAsyncEnumerable<Moment> moments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<Moment> kept = [];
        var flowing = new StringBuilder();
        Moment? flow = null;
        var since = DateTimeOffset.MinValue;
        var started = _clock.GetUtcNow();
        Outcome? outcome = null;

        void Settle()
        {
            if (flow is not null)
            {
                kept.Add(flow is Speech ? new Speech(flowing.ToString()) : new Thought(flowing.ToString(), _clock.GetUtcNow() - since));
                flowing.Clear();
                flow = null;
            }
        }

        void Flow(Moment kind, string text)
        {
            if (flow is not null && flow.GetType() != kind.GetType())
            {
                Settle();
            }

            if (flow is null && kind is Thought)
            {
                since = _clock.GetUtcNow();
            }

            flow = kind;
            flowing.Append(text);
        }

        await foreach (var moment in moments.WithCancellation(cancellationToken))
        {
            if (outcome is not null)
            {
                break;
            }

            switch (moment)
            {
                case Speech speech:
                    Flow(speech, speech.Text);
                    break;
                case Thought thought:
                    Flow(thought, thought.Text);
                    break;
                case Plan plan:
                    Settle();
                    kept.RemoveAll(each => each is Plan);
                    kept.Add(plan);
                    break;
                case Act act:
                    Settle();
                    kept.Add(act);
                    break;
                case ActResult result:
                    Settle();
                    kept.Add(result.Output.Length > LongestOutput ? result with { Output = result.Output[..LongestOutput] + "\n…" } : result);
                    break;
                case Outcome ended:
                    outcome = ended;
                    break;
            }

            yield return moment;
        }

        Settle();

        if (outcome is null)
        {
            outcome = new Failed("The agent went silent before finishing its reply.");

            yield return outcome;
        }

        _turns.Add(new Reply(kept, outcome, _clock.GetUtcNow() - started));
    }
}
