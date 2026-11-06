using System.Runtime.CompilerServices;
using System.Text;
using Overworld.Apps.Crafter.Agents;
using Overworld.Apps.Crafter.Agents.Background;
using Overworld.Apps.Crafter.Agents.Options;
using Overworld.Apps.Crafter.Conversations.Decisions;
using Overworld.Apps.Crafter.Conversations.Replies;

namespace Overworld.Apps.Crafter.Conversations;

public sealed class Conversation
{
    private readonly IAgentSession _agent;

    private readonly List<Turn> _turns;

    public Conversation(IAgentSession agent, IEnumerable<Turn> history)
    {
        _agent = agent;
        _turns = [.. history];

        if (agent is IWorksInBackground background)
        {
            background.JobsChanged += (_, jobs) => JobsChanged?.Invoke(this, jobs);
            background.Woke += (_, replies) => Woke?.Invoke(this, Recorded(replies));
        }
    }

    public Conversation(IAgentSession agent)
        : this(agent, [])
    {
    }

    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Reply>>? Woke;

    public IReadOnlyList<Turn> Turns => _turns;

    public string? SessionId => _agent.SessionId;

    public bool Runs(IAgentSession session) => ReferenceEquals(_agent, session);

    public IReadOnlyList<Job> Jobs => _agent is IWorksInBackground background ? background.Jobs : [];

    public void Decide(string request, Decision decision) => _agent.Decide(request, decision);

    public string? Output(string job) => _agent is IWorksInBackground background ? background.Output(job) : null;

    public void Stop(string job)
    {
        if (_agent is IWorksInBackground background)
        {
            background.Stop(job);
        }
    }

    public void Dismiss()
    {
        if (_agent is IWorksInBackground background)
        {
            background.Dismiss();
        }
    }

    public IAsyncEnumerable<Reply> Ask(Prompt prompt, Configuration preferred, CancellationToken cancellationToken = default)
    {
        _turns.Add(new Asked(prompt));

        return Recorded(_agent.Answer(prompt, preferred, cancellationToken), cancellationToken);
    }

    private async IAsyncEnumerable<Reply> Recorded(IAsyncEnumerable<Reply> replies, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var answer = new StringBuilder();
        var writing = new StringBuilder();
        List<Reply> steps = [];
        Ending? ending = null;

        void Wrap()
        {
            if (writing.Length > 0)
            {
                steps.Add(new Wrote(writing.ToString()));
                writing.Clear();
            }
        }

        await foreach (var reply in replies.WithCancellation(cancellationToken))
        {
            switch (reply)
            {
                case Wrote wrote:
                    answer.Append(wrote.Fragment);
                    writing.Append(wrote.Fragment);
                    break;
                case Acting acting:
                    Wrap();
                    steps.Add(acting);
                    break;
                case Acted acted:
                    Wrap();
                    steps.Add(acted.Output.Length > Answered.LongestOutput ? acted with { Output = acted.Output[..Answered.LongestOutput] + "\n…" } : acted);
                    break;
                case Ending ended:
                    ending = ended;
                    break;
            }

            yield return reply;
        }

        Wrap();

        if (ending is null)
        {
            ending = new Failed("The agent went silent before finishing its answer.");

            yield return ending;
        }

        // An answer of words alone is its text; steps are kept only when tools came between them.
        _turns.Add(new Answered(answer.ToString(), ending) { Steps = steps.Any(step => step is not Wrote) ? steps : [] });
    }
}
