using Overworld.Allay.Agents;
using Overworld.Allay.Bells;
using Overworld.Allay.Conversations;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Remote;

internal sealed class HostedChat : IDisposable
{
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(1);

    private readonly Transcripts _transcripts;

    private readonly Lock _gate = new();

    private readonly List<Moment> _pending = [];

    private readonly Timer _saving;

    private readonly Dictionary<string, Request> _requests = [];

    private readonly Dictionary<string, string> _rung = [];

    private readonly Ringer? _ringer;

    private readonly string _context;

    private readonly string? _color;

    private readonly Action<IReadOnlyList<string>> _launch;

    private string? _stream;

    private CancellationTokenSource? _asking;

    public HostedChat(string id, ISession session, IEnumerable<Turn> turns, bool keep, Transcripts transcripts, Ringer? ringer = null, string context = "", string? color = null, Action<IReadOnlyList<string>>? launch = null)
    {
        _ringer = ringer;
        _context = context;
        _color = color;
        _launch = launch ?? (_ => { });
        Id = id;
        Session = session;
        Keep = keep;
        _transcripts = transcripts;
        Conversation = new Conversation(session, turns);
        _saving = new Timer(_ => _ = SaveAsync());
        Conversation.Woke += (_, moments) => _ = HearAsync(Op.Woke, moments, null);
        session.JobsChanged += (_, jobs) => Said?.Invoke(new Envelope(Op.Jobs, Id, Jobs: jobs));
    }

    public event Action<Envelope>? Said;

    public string Id { get; }

    public ISession Session { get; }

    public Conversation Conversation { get; }

    public bool Keep { get; }

    public IReadOnlyList<string>? Opener { get; set; }

    public int Watchers { get; private set; }

    public void Watch(int change)
    {
        var was = Watchers;
        Watchers = Math.Max(0, Watchers + change);

        if (was > 0 && Watchers == 0)
        {
            _ = RingAllAsync();
        }
        else if (was == 0 && Watchers > 0)
        {
            _ = WithdrawAllAsync();
        }
    }

    public void Decide(string request, Decision decision)
    {
        Session.Decide(request, decision);
        _ = SettleAsync(request);
    }

    public Envelope Opened(int? number)
    {
        lock (_gate)
        {
            return new Envelope(
                Op.Opened,
                Id,
                number,
                Session: Session.Id,
                Turns: Conversation.Turns.ToArray(),
                Stream: _stream,
                Pending: _pending.ToArray(),
                Jobs: Session.Jobs);
        }
    }

    public void Ask(Prompt prompt, Setup setup)
    {
        var asking = new CancellationTokenSource();
        _asking = asking;
        _ = HearAsync(Op.Reply, Conversation.AskAsync(prompt, setup, asking.Token), asking);
    }

    public void Cancel() => _asking?.Cancel();

    public void Withdraw(Prompt prompt) => Conversation.Withdraw(prompt);

    public Task SaveAsync()
    {
        if (!Keep)
        {
            return Task.CompletedTask;
        }

        Transcript transcript;

        lock (_gate)
        {
            transcript = new Transcript(Session.Id, Conversation.Turns.ToArray(), _pending.ToArray());
        }

        return _transcripts.SaveAsync(Id, transcript, CancellationToken.None);
    }

    public void End()
    {
        Dispose();
        Session.End();
    }

    public void Dispose() => _saving.Dispose();

    private async Task RingAsync(Request request)
    {
        if (_ringer is null)
        {
            return;
        }

        lock (_gate)
        {
            if (_rung.ContainsKey(request.Id))
            {
                return;
            }
        }

        var notice = await _ringer.RingAsync(request, new Ringing(_context, _color, false, decision => Decide(request.Id, decision), Open), CancellationToken.None);

        if (notice is not null)
        {
            lock (_gate)
            {
                _rung[request.Id] = notice;
            }
        }
    }

    private async Task RingAllAsync()
    {
        Request[] open;

        lock (_gate)
        {
            open = [.. _requests.Values];
        }

        foreach (var request in open)
        {
            await RingAsync(request);
        }
    }

    private async Task SettleAsync(string request)
    {
        string? notice;

        lock (_gate)
        {
            _requests.Remove(request);
            _rung.Remove(request, out notice);
        }

        if (notice is not null && _ringer is not null)
        {
            await _ringer.WithdrawAsync(notice, CancellationToken.None);
        }
    }

    private async Task WithdrawAllAsync()
    {
        string[] notices;

        lock (_gate)
        {
            notices = [.. _rung.Values];
            _rung.Clear();
        }

        foreach (var notice in notices)
        {
            if (_ringer is not null)
            {
                await _ringer.WithdrawAsync(notice, CancellationToken.None);
            }
        }
    }

    private async Task HearAsync(string stream, IAsyncEnumerable<Moment> moments, CancellationTokenSource? asking)
    {
        lock (_gate)
        {
            _stream = stream;
            _pending.Clear();
        }

        try
        {
            await foreach (var moment in moments)
            {
                lock (_gate)
                {
                    _pending.Add(moment);
                }

                switch (moment)
                {
                    case Request request:
                        lock (_gate)
                        {
                            _requests[request.Id] = request;
                        }

                        if (Watchers == 0)
                        {
                            await RingAsync(request);
                        }

                        break;
                    case Withdrawn withdrawn:
                        await SettleAsync(withdrawn.Id);
                        break;
                }

                Said?.Invoke(new Envelope(Op.Heard, Id, Stream: stream, Moment: moment));
                _saving.Change(Settle, Timeout.InfiniteTimeSpan);
            }
        }
        catch (Exception broken) when (broken is not OperationCanceledException)
        {
            Said?.Invoke(new Envelope(Op.Heard, Id, Stream: stream, Moment: new Failed($"The agent broke off: {broken.Message}")));
        }
        finally
        {
            lock (_gate)
            {
                _stream = null;
                _pending.Clear();
            }

            await WithdrawAllAsync();

            lock (_gate)
            {
                _requests.Clear();
            }

            if (ReferenceEquals(_asking, asking))
            {
                _asking = null;
            }

            asking?.Dispose();
            Said?.Invoke(new Envelope(Op.Known, Id, Session: Session.Id));
            await SaveAsync();
        }
    }

    private void Open()
    {
        if (Opener is [_, ..] opener)
        {
            _launch(opener);
        }
    }
}
