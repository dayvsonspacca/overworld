using System.Collections.Concurrent;
using System.Net.Sockets;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Bedrock.Results;

namespace Overworld.Allay.Remote;

public sealed class AllayLink(string path) : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, RemoteSession> _sessions = new();

    private readonly ConcurrentDictionary<int, TaskCompletionSource<Envelope>> _waiting = new();

    private readonly SemaphoreSlim _connecting = new(1, 1);

    private readonly Lock _writing = new();

    private Socket? _socket;

    private StreamWriter? _writer;

    private int _numbers;

    public AllayLink()
        : this(AllaySocket.Path)
    {
    }

    public bool IsServing() => IsServing(path);

    public static bool IsServing(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            probe.Connect(new UnixDomainSocketEndPoint(path));

            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public async Task<Result<Opened>> OpenAsync(string chat, string agent, Brief brief, string? resume, IReadOnlyList<Turn> history, bool keep, CancellationToken cancellationToken, IReadOnlyList<string>? opener = null)
    {
        if (!await ConnectAsync(cancellationToken))
        {
            return new Error("allay.unreachable", "Allay is not running.");
        }

        var session = _sessions.GetOrAdd(chat, id => new RemoteSession(this, id));
        var reply = await RequestAsync(new Envelope(Op.Open, chat, Agent: agent, Brief: brief, Session: resume, Turns: history, Keep: keep, Opener: opener), cancellationToken);

        if (reply is null)
        {
            return new Error("allay.silent", "Allay did not answer in time.");
        }

        if (reply.Error is { } error)
        {
            return new Error("allay.refused", error);
        }

        session.Open(reply);

        return new Opened(session, reply.Turns ?? []);
    }

    public void Dispose()
    {
        Close();
        _connecting.Dispose();
    }

    internal void Send(Envelope envelope)
    {
        lock (_writing)
        {
            try
            {
                _writer?.WriteLine(Wire.Write(envelope));
            }
            catch (Exception broken) when (broken is IOException or ObjectDisposedException)
            {
            }
        }
    }

    internal async Task<Envelope?> RequestAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        var number = Interlocked.Increment(ref _numbers);
        var waiting = new TaskCompletionSource<Envelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting[number] = waiting;
        Send(envelope with { Number = number });

        try
        {
            return await waiting.Task.WaitAsync(Patience, cancellationToken);
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            _waiting.TryRemove(number, out _);
        }
    }

    internal void Forget(string chat) => _sessions.TryRemove(chat, out _);

    private async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        if (_socket is { Connected: true })
        {
            return true;
        }

        await _connecting.WaitAsync(cancellationToken);

        try
        {
            if (_socket is { Connected: true })
            {
                return true;
            }

            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            try
            {
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken);
            }
            catch (SocketException)
            {
                socket.Dispose();

                return false;
            }

            var stream = new NetworkStream(socket, ownsSocket: true);
            _writer = new StreamWriter(stream) { AutoFlush = true };
            _socket = socket;
            _ = ListenAsync(new StreamReader(stream));

            return true;
        }
        finally
        {
            _connecting.Release();
        }
    }

    private async Task ListenAsync(StreamReader reader)
    {
        try
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                if (Wire.Read(line) is { } envelope)
                {
                    Route(envelope);
                }
            }
        }
        catch (Exception closed) when (closed is IOException or ObjectDisposedException)
        {
        }

        Close();

        foreach (var session in _sessions.Values)
        {
            session.Lost();
        }

        _sessions.Clear();
    }

    private void Route(Envelope envelope)
    {
        if (envelope.Number is { } number && _waiting.TryRemove(number, out var waiting))
        {
            waiting.TrySetResult(envelope);

            return;
        }

        if (envelope.Chat is { } chat && _sessions.TryGetValue(chat, out var session))
        {
            session.Hear(envelope);
        }
    }

    private void Close()
    {
        lock (_writing)
        {
            _writer?.Dispose();
            _writer = null;
        }

        _socket?.Dispose();
        _socket = null;
    }
}
