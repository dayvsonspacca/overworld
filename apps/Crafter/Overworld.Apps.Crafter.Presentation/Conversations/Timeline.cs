using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using Overworld.Allay.Conversations;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class Timeline
{
    public const int Page = 15;

    private readonly Dictionary<string, ActEntry> _acts = [];

    private readonly Dictionary<string, RequestEntry> _requests = [];

    private readonly Action<string, Decision> _decide;

    private readonly Func<string, Picture?> _find;

    private readonly IReadOnlyList<ILender> _lenders;

    private readonly HashSet<string> _lent = [];

    private PlanEntry? _plan;

    public Timeline(Action<string, Decision>? decide = null, Func<string, Picture?>? find = null, IReadOnlyList<ILender>? lenders = null)
    {
        _decide = decide ?? ((_, _) => { });
        _find = find ?? (_ => null);
        _lenders = lenders ?? [];
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

    // Puts earlier entries away again, never below a page; what is left is still the latest.
    public int ShowFewer(int count)
    {
        var fewer = Math.Clamp(count, 0, Math.Max(0, Shown.Count - Page));

        for (var at = 0; at < fewer; at++)
        {
            Shown.RemoveAt(0);
        }

        return fewer;
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

    public void Withdraw(Prompt prompt)
    {
        var at = Entries.ToList().FindLastIndex(entry => entry is PromptEntry shown && shown.Prompt == prompt);

        while (at >= 0 && Entries.Count > at)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
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
                Show(done, result);
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

    private void Show(ActEntry act, ActResult result)
    {
        if (!result.Succeeded || !result.Output.TrimStart().StartsWith('{'))
        {
            return;
        }

        try
        {
            using var said = JsonDocument.Parse(result.Output);
            var root = said.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if ((Picture(act, root) as Entry ?? Loan(root)) is { } shown)
            {
                Entries.Insert(Entries.IndexOf(act) + 1, shown);
            }
        }
        catch (JsonException)
        {
        }
    }

    private PictureEntry? Picture(ActEntry act, JsonElement root)
    {
        if (act.Kind != ActKind.Tool
            || !root.TryGetProperty("shownPicture", out var name) || name.GetString() is not { } kept
            || _find(kept) is not { } picture)
        {
            return null;
        }

        var file = root.TryGetProperty("file", out var named) ? named.GetString() ?? kept : kept;
        var caption = root.TryGetProperty("caption", out var told) ? told.GetString() : null;

        return new PictureEntry(new PictureView(picture, caption ?? file), file, caption);
    }

    private LoanEntry? Loan(JsonElement root)
    {
        foreach (var lender in _lenders)
        {
            if (lender.Lend(root) is { } loan)
            {
                return _lent.Add(loan.Key) ? new LoanEntry(loan) : null;
            }
        }

        return null;
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
