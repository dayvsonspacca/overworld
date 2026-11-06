using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Folders;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Presentation.Composing;
using Overworld.Apps.Crafter.Presentation.Jobs;
using Overworld.Apps.Crafter.Proposals;
using Overworld.Apps.Crafter.Sessions;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

[SuppressMessage(
    "Reliability",
    "CA1001",
    Justification = "The stop source lives only while the agent works on what was sent and is disposed when it is done.")]
public sealed partial class ConversationViewModel : ObservableObject
{
    private static readonly ModelOption Default = new(Crew.DefaultModel, "Default model");

    private readonly string _folder;

    private readonly ChatRecord _record;

    private readonly Workshop _workshop;

    private readonly Abilities _abilities;

    private IReadOnlyList<Skill> _skills = [];

    private readonly SynchronizationContext? _ui = SynchronizationContext.Current;

    private readonly Queue<Prompt> _queued = new();

    private Chat? _chat;

    private CancellationTokenSource? _stopping;

    private bool _forgotten;

    public ConversationViewModel(string folder, ChatRecord record, Abilities abilities)
    {
        _folder = folder;
        _record = record;
        _abilities = abilities;
        _workshop = abilities.Workshop;
        Mentions = new MentionsViewModel(cancellationToken => ProjectFiles.OfAsync(folder, cancellationToken), () => _skills);
        Attachments.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAttachments));
            SendCommand.NotifyCanExecuteChanged();
        };
        Timeline = new Timeline(Decide, abilities.Pictures.Find);
        Viewer = new ViewerViewModel(picture => abilities.Pictures.Keep(picture) is { } kept ? abilities.Pictures.PathTo(kept) : null);
        Title = record.Title.Text;

        foreach (var turn in record.Turns)
        {
            Timeline.Add(turn);
        }

        Timeline.ShowLatest();
    }

    public event EventHandler? Answered;

    public Timeline Timeline { get; }

    public ViewerViewModel Viewer { get; }

    public ChatRecord Record => _chat?.Record() ?? _record;

    public ObservableCollection<ModelOption> Models { get; } = [Default];

    public MentionsViewModel Mentions { get; }

    public ObservableCollection<AttachmentView> Attachments { get; } = [];

    public bool HasAttachments => Attachments.Count > 0;

    public ObservableCollection<JobViewModel> Jobs { get; } = [];

    public bool HasJobs => Jobs.Count > 0;

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial string Draft { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    public partial bool IsWorking { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQueued), nameof(QueuedLabel))]
    public partial int Queued { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyPropertyChangedFor(nameof(Placeholder))]
    public partial string? Agent { get; private set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModeLabel))]
    public partial Mode Mode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModelLabel))]
    public partial string? Model { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffortLabel), nameof(IsMax))]
    public partial Effort Effort { get; set; }

    public bool HasQueued => Queued > 0;

    public string QueuedLabel => Queued == 1 ? "1 message waiting" : $"{Queued} messages waiting";

    public string Placeholder => IsWorking
        ? "Write to queue a message…"
        : Agent is null ? "No agent to talk to" : $"Ask {Agent}";

    public string ModeLabel => Mode switch
    {
        Mode.Edits => "Edits",
        Mode.Plan => "Plan",
        _ => "Auto",
    };

    public string ModelLabel => Models.FirstOrDefault(option => option.Id == (Model ?? Crew.DefaultModel))?.Label ?? Model ?? Default.Label;

    public string EffortLabel => Effort == Effort.Max ? "Max effort" : "Default effort";

    public bool IsMax => Effort == Effort.Max;

    public Setup Setup => new(Mode, Model, Effort);

    public async Task LoadDeskAsync(CancellationToken cancellationToken)
    {
        var skills = await _abilities.ListProjectSkills.ExecuteAsync(new ListProjectSkills(_folder), cancellationToken);
        _skills = skills.IsSuccess ? skills.Value : [];

        var desk = await _workshop.DeskAsync(_record, cancellationToken);

        if (!desk.IsSuccess)
        {
            Problem = desk.Error.Message;

            return;
        }

        Models.Clear();
        Models.Add(Default);

        foreach (var option in desk.Value.Models)
        {
            Models.Add(option);
        }

        (Mode, Model, Effort) = (desk.Value.Setup.Mode, desk.Value.Setup.Model, desk.Value.Setup.Effort);
        Agent = desk.Value.Agent;
        OnPropertyChanged(nameof(ModelLabel));
    }

    public void Close() => _workshop.Close(_record.Id);

    public void Forget()
    {
        _forgotten = true;
        Stop();
        Close();
    }

    public void Attach(Attachment attachment)
    {
        if (!Attachments.Any(each => each.Attachment.Equals(attachment)))
        {
            Attachments.Add(new AttachmentView(attachment));
        }
    }

    [RelayCommand]
    private void Detach(AttachmentView attachment) => Attachments.Remove(attachment);

    [RelayCommand]
    private void OpenPicture(PictureView picture) =>
        Viewer.Open(
            [
                .. Timeline.Entries.SelectMany(entry => entry switch
                {
                    PromptEntry prompt => prompt.Pictures,
                    PictureEntry shown => [shown.Picture],
                    _ => [],
                }),
            ],
            picture);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var asked = Asking.Of(Draft, _folder, [.. _skills.Select(skill => skill.Id)], [.. Attachments.Select(each => each.Attachment)]);

        if (!asked.IsSuccess)
        {
            Problem = asked.Error.Message;

            return;
        }

        var prompt = asked.Value;
        Problem = null;
        Draft = string.Empty;
        Attachments.Clear();
        Mentions.Close();

        if (IsWorking)
        {
            _queued.Enqueue(prompt);
            Queued = _queued.Count;

            return;
        }

        await TalkAsync(prompt);
    }

    [RelayCommand(CanExecute = nameof(IsWorking))]
    private void Stop()
    {
        _queued.Clear();
        Queued = 0;
        _stopping?.Cancel();
    }

    [RelayCommand]
    private void CycleMode() => Mode = Mode switch
    {
        Mode.Auto => Mode.Edits,
        Mode.Edits => Mode.Plan,
        _ => Mode.Auto,
    };

    [RelayCommand]
    private void ToggleEffort() => Effort = Effort == Effort.Max ? Effort.Default : Effort.Max;

    [RelayCommand]
    private void PickModel(ModelOption option) => Model = option.Id == Crew.DefaultModel ? null : option.Id;

    private bool CanSend() => (Draft.Trim().Length > 0 || Attachments.Count > 0) && Agent is not null;

    private async Task TalkAsync(Prompt prompt)
    {
        _stopping = new CancellationTokenSource();
        var stopped = _stopping.Token;
        Working(true);

        try
        {
            for (var next = prompt; next is not null && !stopped.IsCancellationRequested; next = _queued.TryDequeue(out var queued) ? queued : null)
            {
                Queued = _queued.Count;

                if (!await OpenAsync() || stopped.IsCancellationRequested)
                {
                    return;
                }

                Timeline.Add(next);
                var reply = _chat!.AskAsync(next, Setup, stopped);
                Title = _chat.Title.Text;
                await HearAsync(reply);
            }
        }
        finally
        {
            _stopping.Dispose();
            _stopping = null;
            Working(false);
        }
    }

    private async Task<bool> OpenAsync()
    {
        if (_chat is not null)
        {
            return true;
        }

        var opened = await _workshop.OpenAsync(_folder, _record, AskAsync, CancellationToken.None);

        if (!opened.IsSuccess)
        {
            Problem = opened.Error.Message;

            return false;
        }

        Problem = null;
        _chat = opened.Value;
        _chat.Conversation.Woke += (_, moments) => OnUi(() => _ = WokenAsync(moments));
        _chat.Session.JobsChanged += (_, jobs) => OnUi(() => Rejob(jobs));
        _chat.Retitled += (_, _) => OnUi(() => Title = _chat.Title.Text);
        Rejob(_chat.Session.Jobs);

        return true;
    }

    private async Task WokenAsync(IAsyncEnumerable<Moment> moments)
    {
        Working(true);

        try
        {
            await HearAsync(moments);
        }
        finally
        {
            Working(false);
        }
    }

    private async Task HearAsync(IAsyncEnumerable<Moment> moments)
    {
        try
        {
            await foreach (var moment in moments)
            {
                Timeline.Add(moment);
            }
        }
        catch (Exception broken) when (broken is not OperationCanceledException)
        {
            Timeline.Add(new Failed(broken.Message));
        }
        finally
        {
            Title = _chat!.Title.Text;
        }

        if (_forgotten)
        {
            return;
        }

        var kept = await _workshop.KeepAsync(_folder, _chat!, CancellationToken.None);
        Problem = kept.IsSuccess ? null : kept.Error.Message;
    }

    private void Working(bool working)
    {
        IsWorking = working;
        OnPropertyChanged(nameof(Placeholder));

        if (!working)
        {
            Answered?.Invoke(this, EventArgs.Empty);
        }
    }

    private Task<bool> AskAsync(Proposal proposal, CancellationToken cancellationToken)
    {
        var asked = new TaskCompletionSource<ProposalEntry>(TaskCreationOptions.RunContinuationsAsynchronously);

        OnUi(() =>
        {
            ProposalEntry entry = proposal switch
            {
                AxiomProposal axiom => new AxiomProposalEntry(axiom.Axiom),
                AxiomRemovalProposal removal => new AxiomRemovalProposalEntry(removal.Axiom, removal.Why),
                SummaryProposal summary => new SummaryProposalEntry(summary.Current, summary.Proposed),
                _ => throw new ArgumentOutOfRangeException(nameof(proposal), proposal, "Crafter knows no such proposal."),
            };

            Timeline.Add(entry);
            cancellationToken.Register(() => OnUi(entry.Withdraw));
            asked.SetResult(entry);
        });

        return asked.Task.ContinueWith(entry => entry.Result.Decision, TaskScheduler.Default).Unwrap();
    }

    private void Rejob(IReadOnlyList<Allay.Jobs.Job> jobs)
    {
        var known = Jobs.ToDictionary(job => job.Id);
        Jobs.Clear();

        foreach (var job in jobs)
        {
            Jobs.Add(known.GetValueOrDefault(job.Id) ?? new JobViewModel(job, _chat!.Session));
        }

        OnPropertyChanged(nameof(HasJobs));
    }

    private void Decide(string request, Decision decision) => _chat?.Session.Decide(request, decision);

    private void OnUi(Action act)
    {
        if (_ui is null)
        {
            act();
        }
        else
        {
            _ui.Post(_ => act(), null);
        }
    }
}
