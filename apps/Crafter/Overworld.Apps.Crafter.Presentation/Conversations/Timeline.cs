using System.Collections.ObjectModel;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class Timeline
{
    private readonly Dictionary<string, ActEntry> _acts = [];

    private PlanEntry? _plan;

    public ObservableCollection<Entry> Entries { get; } = [];

    public void Add(Turn turn)
    {
        switch (turn)
        {
            case Prompt prompt:
                Add(prompt);
                break;
            case Reply reply:
                foreach (var moment in reply.Moments)
                {
                    Add(moment);
                }

                Add(reply.Outcome);
                break;
        }
    }

    public void Add(Prompt prompt)
    {
        _plan = null;
        _acts.Clear();
        Entries.Add(new PromptEntry(prompt));
    }

    public void Add(Moment moment)
    {
        switch (moment)
        {
            case Speech speech:
                Flow(speech.Text, text => new SpeechEntry(text));
                break;
            case Thought thought:
                Flow(thought.Text, text => new ThoughtEntry(text));
                break;
            case Plan plan when _plan is not null:
                _plan.Steps = plan.Steps;
                break;
            case Plan plan:
                _plan = new PlanEntry(plan);
                Entries.Add(_plan);
                break;
            case Act act:
                var entry = new ActEntry(act);
                _acts[act.Id] = entry;
                Entries.Add(entry);
                break;
            case ActResult result when _acts.TryGetValue(result.Id, out var done):
                done.Settle(result);
                break;
            case Notice notice:
                Entries.Add(new NoticeEntry(notice));
                break;
            case Outcome outcome:
                Entries.Add(new OutcomeEntry(outcome));
                break;
        }
    }

    private void Flow<T>(string text, Func<string, T> start)
        where T : FlowingEntry
    {
        if (Entries.LastOrDefault() is T flowing)
        {
            flowing.Append(text);
        }
        else
        {
            Entries.Add(start(text));
        }
    }
}
