using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Bells;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Presentation.Conversations;
using Overworld.Allay.Remote;
using Overworld.Allay.Visits;
using Overworld.Bedrock.Lending;

namespace Overworld.Allay.Presentation.Chats;

[SuppressMessage(
    "Reliability",
    "CA1001",
    Justification = "The stop source lives only while the agent works on what was sent and is disposed when it is done.")]
public sealed partial class ChatViewModel : ObservableObject
{
    private readonly Lobby _lobby;

    private readonly Func<CancellationToken, Task<Brief>> _brief;

    private readonly IReadOnlyList<ILender> _lenders;

    private readonly Ringer? _ringer;

    private readonly string _context;

    private readonly Dictionary<string, string> _rung = [];

    private readonly SynchronizationContext? _ui = SynchronizationContext.Current;

    private Visit? _visit;

    private CancellationTokenSource? _stopping;

    private bool _heard;

    public ChatViewModel(
        Lobby lobby,
        Func<CancellationToken, Task<Brief>> brief,
        IReadOnlyList<ILender>? lenders = null,
        IReadOnlyList<string>? suggestions = null,
        Ringer? ringer = null,
        string context = "")
    {
        _lobby = lobby;
        _brief = brief;
        _lenders = lenders ?? [];
        _ringer = ringer;
        _context = context;
        Suggestions = suggestions ?? [];
        Timeline = Fresh();
        Pictures.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasPictures));
            SendCommand.NotifyCanExecuteChanged();
        };
    }

    public event EventHandler? Summoned;

    public IReadOnlyList<string> Suggestions { get; }

    public bool IsAttended { get; set; }

    public Pulse Pulse { get; } = new();

    public ObservableCollection<PictureView> Pictures { get; } = [];

    public bool HasPictures => Pictures.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial Timeline Timeline { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial string Draft { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand), nameof(StopCommand), nameof(RestartCommand))]
    [NotifyPropertyChangedFor(nameof(Placeholder))]
    public partial bool IsWorking { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyPropertyChangedFor(nameof(Placeholder))]
    public partial string? Agent { get; private set; }

    [ObservableProperty]
    public partial string? Color { get; private set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    [ObservableProperty]
    public partial LoanEntry? Spread { get; private set; }

    [ObservableProperty]
    public partial bool IsOpen { get; private set; }

    public bool IsEmpty => Timeline.Entries.Count == 0;

    public string Placeholder => IsWorking
        ? "The agent is working…"
        : Agent is null ? "No agent to talk to" : $"Ask {Agent}";

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var agent = await _lobby.AgentAsync(cancellationToken);

        if (!agent.IsSuccess)
        {
            Problem = agent.Error.Message;

            return;
        }

        Problem = null;
        Agent = agent.Value.Name;
        Color = agent.Value.Color;
    }

    public void Attach(string mediaType, byte[] data)
    {
        var picture = new Picture(mediaType, data);

        if (!Pictures.Any(each => each.Picture.Equals(picture)))
        {
            Pictures.Add(new PictureView(picture, "Pasted by you"));
        }
    }

    public void End()
    {
        Unring();
        _stopping?.Cancel();
        _visit?.Conversation.Session.End();
        _visit = null;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var prompt = new Prompt(Draft.Trim(), [.. Pictures.Select(each => each.Picture)], []);
        Draft = string.Empty;
        Pictures.Clear();
        Problem = null;
        _stopping = new CancellationTokenSource();
        var stopped = _stopping.Token;
        Working(true);

        try
        {
            if (await OpenAsync() is not { } visit || stopped.IsCancellationRequested)
            {
                Restore(prompt);

                return;
            }

            _heard = false;
            Timeline.Add(prompt);
            OnPropertyChanged(nameof(IsEmpty));
            Pulse.Start();
            await HearAsync(visit, prompt, visit.Conversation.AskAsync(prompt, visit.Setup, stopped));
        }
        finally
        {
            _stopping.Dispose();
            _stopping = null;
            Working(false);
        }
    }

    [RelayCommand(CanExecute = nameof(IsWorking))]
    private void Stop() => _stopping?.Cancel();

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void Restart()
    {
        End();
        Timeline = Fresh();
        Spread = null;
        Problem = null;
    }

    [RelayCommand]
    private void Detach(PictureView picture) => Pictures.Remove(picture);

    [RelayCommand]
    private void Toggle() => IsOpen = !IsOpen;

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        Spread = null;
    }

    [RelayCommand]
    private void Suggest(string suggestion) => Draft = suggestion;

    [RelayCommand]
    private void ExpandLoan(LoanEntry loan) => Spread = loan;

    [RelayCommand]
    private void CloseSpread() => Spread = null;

    private bool CanSend() => (Draft.Trim().Length > 0 || Pictures.Count > 0) && Agent is not null && !IsWorking;

    private bool CanRestart() => !IsWorking;

    private Timeline Fresh() => new(Decide, null, _lenders);

    private async Task<Visit?> OpenAsync()
    {
        if (_visit is not null)
        {
            return _visit;
        }

        var opened = await _lobby.OpenAsync(await _brief(CancellationToken.None), CancellationToken.None);

        if (!opened.IsSuccess)
        {
            Problem = opened.Error.Message;

            return null;
        }

        _visit = opened.Value;
        (Agent, Color) = (_visit.Agent, _visit.Color);

        return _visit;
    }

    private async Task HearAsync(Visit visit, Prompt prompt, IAsyncEnumerable<Moment> moments)
    {
        try
        {
            await foreach (var moment in moments)
            {
                _heard |= moment is not Outcome;
                Timeline.Add(moment);
                Pulse.Heard(moment);
                Ring(moment);
            }
        }
        catch (OperationCanceledException) when (!_heard)
        {
            Withdraw(visit, prompt);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception broken)
        {
            Timeline.Add(new Failed(broken.Message));
        }
    }

    private void Withdraw(Visit visit, Prompt prompt)
    {
        Timeline.Withdraw(prompt);
        visit.Conversation.Withdraw(prompt);

        if (visit.Conversation.Session is RemoteSession remote)
        {
            remote.Withdraw(prompt);
        }

        Restore(prompt);
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void Restore(Prompt prompt)
    {
        Draft = prompt.Text;

        foreach (var picture in prompt.Attachments.OfType<Picture>())
        {
            Attach(picture.MediaType, picture.Data);
        }
    }

    private void Working(bool working)
    {
        IsWorking = working;

        if (!working)
        {
            Pulse.Stop();
        }
    }

    private void Decide(string request, Decision decision)
    {
        _visit?.Conversation.Session.Decide(request, decision);
        Pulse.Resume();

        if (_rung.Remove(request, out var notice))
        {
            _ = _ringer!.WithdrawAsync(notice, CancellationToken.None);
        }
    }

    private void Ring(Moment moment)
    {
        if (_ringer is null)
        {
            return;
        }

        switch (moment)
        {
            case Request request when Timeline.Request(request.Id) is { IsOpen: true } entry:
                _ = RingAsync(_ringer, request, entry);
                break;
            case Withdrawn withdrawn when _rung.Remove(withdrawn.Id, out var notice):
                _ = _ringer.WithdrawAsync(notice, CancellationToken.None);
                break;
        }
    }

    private async Task RingAsync(Ringer ringer, Request request, RequestEntry entry)
    {
        var ringing = new Ringing(
            _context,
            Color,
            IsAttended && IsOpen,
            decision => OnUi(() => entry.Settle(decision)),
            () => OnUi(Summon));

        var visit = _visit;

        if (await ringer.RingAsync(request, ringing, CancellationToken.None) is not { } notice)
        {
            return;
        }

        if (entry.IsOpen && visit is not null && visit == _visit)
        {
            _rung[request.Id] = notice;
        }
        else
        {
            await ringer.WithdrawAsync(notice, CancellationToken.None);
        }
    }

    private void Summon()
    {
        IsOpen = true;
        Summoned?.Invoke(this, EventArgs.Empty);
    }

    private void Unring()
    {
        foreach (var notice in _rung.Values)
        {
            _ = _ringer?.WithdrawAsync(notice, CancellationToken.None);
        }

        _rung.Clear();
    }

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
