using System.Runtime.CompilerServices;
using System.Text;
using Overworld.Allay.Agents;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Conversations;

public sealed class Conversation
{
    public const int LongestOutput = 4000;

    private readonly List<Turn> _turns;

    public Conversation(ISession session, IEnumerable<Turn> history)
    {
        Session = session;
        _turns = [.. history];
        session.Woke += (_, moments) => Woke?.Invoke(this, Recorded(moments));
    }

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke;

    public ISession Session { get; }

    public IReadOnlyList<Turn> Turns => _turns;

    public IAsyncEnumerable<Moment> AskAsync(Prompt prompt, Setup setup, CancellationToken cancellationToken)
    {
        _turns.Add(prompt);

        return Recorded(Session.ReplyAsync(prompt, setup, cancellationToken), cancellationToken);
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
        Outcome? outcome = null;

        void Settle()
        {
            if (flow is not null)
            {
                kept.Add(flow is Speech ? new Speech(flowing.ToString()) : new Thought(flowing.ToString()));
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

        _turns.Add(new Reply(kept, outcome));
    }
}
