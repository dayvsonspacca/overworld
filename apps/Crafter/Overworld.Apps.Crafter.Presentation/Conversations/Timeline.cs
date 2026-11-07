using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class Timeline
{
    public const int Page = 15;

    private readonly Dictionary<string, ActEntry> _acts = [];

    private readonly Dictionary<string, RequestEntry> _requests = [];

    private readonly Action<string, Decision> _decide;

    private readonly Func<string, Picture?> _find;

    private PlanEntry? _plan;

    public Timeline(Action<string, Decision>? decide = null, Func<string, Picture?>? find = null)
    {
        _decide = decide ?? ((_, _) => { });
        _find = find ?? (_ => null);
        Entries.CollectionChanged += Follow;
    }

    public ObservableCollection<Entry> Entries { get; } = [];

    public RequestEntry? Request(string id) => _requests.GetValueOrDefault(id);

    public ObservableCollection<Entry> Shown { get; } = [];

    public bool HasEarlier => Shown.Count < Entries.Count;

    public void ShowLatest()
    {
        Shown.Clear();

        foreach (var entry in Entries.TakeLast(Page))
        {
            Shown.Add(entry);
        }
    }

    public int ShowEarlier()
    {
        var hidden = Entries.Count - Shown.Count;
        var more = Math.Min(Page, hidden);

        for (var at = hidden - 1; at >= hidden - more; at--)
        {
            Shown.Insert(0, Entries[at]);
        }

        return more;
    }

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
        _requests.Clear();
        Entries.Add(new PromptEntry(prompt));
    }

    public void Add(ProposalEntry proposal) => Entries.Add(proposal);

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
                Picture(done, result);
                break;
            case Permission permission:
                Ask(new PermissionEntry(permission, _decide));
                break;
            case Questions questions:
                Ask(new QuestionsEntry(questions, _decide));
                break;
            case Withdrawn withdrawn when _requests.TryGetValue(withdrawn.Id, out var request):
                request.Withdraw();
                break;
            case Notice notice:
                Entries.Add(new NoticeEntry(notice));
                break;
            case Outcome outcome:
                Entries.Add(new OutcomeEntry(outcome));
                break;
        }
    }

    private void Picture(ActEntry act, ActResult result)
    {
        if (act.Kind != ActKind.Tool || !result.Succeeded || !result.Output.TrimStart().StartsWith('{'))
        {
            return;
        }

        try
        {
            using var said = JsonDocument.Parse(result.Output);
            var root = said.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("shownPicture", out var name) && name.GetString() is { } kept
                && _find(kept) is { } picture)
            {
                var file = root.TryGetProperty("file", out var named) ? named.GetString() ?? kept : kept;
                var caption = root.TryGetProperty("caption", out var told) ? told.GetString() : null;
                Entries.Insert(Entries.IndexOf(act) + 1, new PictureEntry(new PictureView(picture, caption ?? file), file, caption));
            }
        }
        catch (JsonException)
        {
        }
    }

    private void Follow(object? sender, NotifyCollectionChangedEventArgs change)
    {
        if (change is { Action: NotifyCollectionChangedAction.Add, NewItems: { } added, NewStartingIndex: var at })
        {
            var hidden = Entries.Count - added.Count - Shown.Count;

            foreach (Entry entry in added)
            {
                if (at >= hidden)
                {
                    Shown.Insert(at - hidden, entry);
                }

                at++;
            }
        }
        else
        {
            ShowLatest();
        }
    }

    private void Ask(RequestEntry request)
    {
        _requests[request.Id] = request;
        Entries.Add(request);
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
