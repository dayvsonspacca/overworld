using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Overworld.Allay.Agents;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Remote;

public sealed class RemoteSession : ISession
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(2);

    private readonly AllayLink _link;

    private readonly string _chat;

    private readonly Lock _gate = new();

    private readonly List<IAsyncEnumerable<Moment>> _held = [];

    private EventHandler<IAsyncEnumerable<Moment>>? _woke;

    private Channel<Moment>? _reply;

    private Channel<Moment>? _waking;

    internal RemoteSession(AllayLink link, string chat)
    {
        _link = link;
        _chat = chat;
    }

    public event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    public event EventHandler<IAsyncEnumerable<Moment>>? Woke
    {
        add
        {
            IAsyncEnumerable<Moment>[] held;

            lock (_gate)
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
            lock (_gate)
            {
                _woke -= value;
            }
        }
    }

    public string? Id { get; private set; }

    public IReadOnlyList<Job> Jobs { get; private set; } = [];

    public async IAsyncEnumerable<Moment> ReplyAsync(Prompt prompt, Setup setup, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reply = Channel.CreateUnbounded<Moment>();

        lock (_gate)
        {
            _reply = reply;
        }

        _link.Send(new Envelope(Op.Ask, _chat, Prompt: prompt, Setup: setup));

        await using var stopping = cancellationToken.Register(() => _link.Send(new Envelope(Op.Cancel, _chat)));

        await foreach (var moment in reply.Reader.ReadAllAsync(CancellationToken.None))
        {
            yield return moment;
        }
    }

    public void Rebrief(Brief brief) => _link.Send(new Envelope(Op.Rebrief, _chat, Brief: brief));

    public void Decide(string request, Decision decision) => _link.Send(new Envelope(Op.Decide, _chat, Request: request, Decision: decision));

    public string? Output(string job) =>
        _link.RequestAsync(new Envelope(Op.Output, _chat, Job: job), CancellationToken.None).WaitAsync(Patience).ContinueWith(asked => asked.IsCompletedSuccessfully ? asked.Result?.Output : null, TaskScheduler.Default).GetAwaiter().GetResult();

    public void Stop(string job) => _link.Send(new Envelope(Op.Stop, _chat, Job: job));

    public void Withdraw(Prompt prompt) => _link.Send(new Envelope(Op.Withdraw, _chat, Prompt: prompt));

    public void Leave()
    {
        _link.Send(new Envelope(Op.Leave, _chat));
        _link.Forget(_chat);
    }

    public void End()
    {
        _link.Send(new Envelope(Op.End, _chat));
        _link.Forget(_chat);
    }

    internal void Open(Envelope opened)
    {
        Id = opened.Session;
        Jobs = opened.Jobs ?? [];

        if (opened.Stream is null)
        {
            return;
        }

        var resumed = Channel.CreateUnbounded<Moment>();

        foreach (var moment in opened.Pending ?? [])
        {
            resumed.Writer.TryWrite(moment);
        }

        lock (_gate)
        {
            _waking = resumed;
        }

        Wake(resumed.Reader.ReadAllAsync());
    }

    internal void Hear(Envelope envelope)
    {
        switch (envelope.Op)
        {
            case Op.Heard when envelope.Moment is { } moment:
                Heard(envelope.Stream, moment);
                break;
            case Op.Known:
                Id = envelope.Session ?? Id;
                break;
            case Op.Jobs:
                Jobs = envelope.Jobs ?? [];
                JobsChanged?.Invoke(this, Jobs);
                break;
        }
    }

    internal void Lost()
    {
        Channel<Moment>?[] open;

        lock (_gate)
        {
            open = [_reply, _waking];
            _reply = null;
            _waking = null;
        }

        foreach (var channel in open)
        {
            channel?.Writer.TryWrite(new Failed("Allay stopped answering. Open the chat again once it is back."));
            channel?.Writer.TryComplete();
        }
    }

    private void Heard(string? stream, Moment moment)
    {
        Channel<Moment>? target;
        var woke = false;

        lock (_gate)
        {
            if (stream == Op.Reply && _reply is not null)
            {
                target = _reply;
            }
            else if (_waking is not null)
            {
                target = _waking;
            }
            else
            {
                target = _waking = Channel.CreateUnbounded<Moment>();
                woke = true;
            }

            if (moment is Outcome)
            {
                if (ReferenceEquals(target, _reply))
                {
                    _reply = null;
                }
                else
                {
                    _waking = null;
                }
            }
        }

        target.Writer.TryWrite(moment);

        if (moment is Outcome)
        {
            target.Writer.TryComplete();
        }

        if (woke)
        {
            Wake(target.Reader.ReadAllAsync());
        }
    }

    private void Wake(IAsyncEnumerable<Moment> moments)
    {
        EventHandler<IAsyncEnumerable<Moment>>? woke;

        lock (_gate)
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
}
