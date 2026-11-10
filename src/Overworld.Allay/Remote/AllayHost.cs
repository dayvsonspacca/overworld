using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Overworld.Allay.Bells;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Redstone;

namespace Overworld.Allay.Remote;

public sealed partial class AllayHost(Crew crew, Transcripts transcripts, ILogger<AllayHost> logger, string? path = null, Ringer? ringer = null, Action<IReadOnlyList<string>>? launch = null) : IDisposable
{
    private readonly ConcurrentDictionary<string, HostedChat> _chats = new();

    private readonly SemaphoreSlim _opening = new(1, 1);

    private readonly string _path = path ?? AllaySocket.Path;

    private Socket? _listener;

    public void Start()
    {
        var endpoint = new UnixDomainSocketEndPoint(_path);

        if (File.Exists(_path))
        {
            if (AllayLink.IsServing(_path))
            {
                throw new AlreadyRunningException(_path);
            }

            File.Delete(_path);
        }

        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            listener.Bind(endpoint);
            listener.Listen();
        }
        catch
        {
            listener.Dispose();
            throw;
        }

        _listener = listener;
        LogListening(_path);
    }

    public async Task RunAsync(CancellationToken token)
    {
        var listener = _listener ?? throw new InvalidOperationException("Start the host before running it.");

        try
        {
            while (true)
            {
                var client = await listener.AcceptAsync(token);
                _ = ServeAsync(client, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            foreach (var chat in _chats.Values)
            {
                await chat.SaveAsync();
                chat.End();
            }
        }
    }

    public void Dispose()
    {
        _opening.Dispose();

        if (_listener is null)
        {
            return;
        }

        _listener.Dispose();
        File.Delete(_path);
    }

    private async Task ServeAsync(Socket client, CancellationToken token)
    {
        var outgoing = Channel.CreateUnbounded<Envelope>(new UnboundedChannelOptions { SingleReader = true });
        Dictionary<string, Action<Envelope>> watching = [];

        await using var stream = new NetworkStream(client, ownsSocket: true);
        using var reader = new StreamReader(stream);
        await using var writer = new StreamWriter(stream) { AutoFlush = true };
        var writing = WriteAsync(writer, outgoing.Reader, token);

        try
        {
            while (await reader.ReadLineAsync(token) is { } line)
            {
                if (Wire.Read(line, out var problem) is { } envelope)
                {
                    await HandleAsync(envelope, outgoing.Writer, watching, token);
                }
                else
                {
                    LogUnreadable(problem);
                }
            }
        }
        catch (Exception closed) when (closed is OperationCanceledException or IOException)
        {
        }
        finally
        {
            foreach (var chat in watching.Keys.ToArray())
            {
                Unwatch(chat, watching);
            }

            outgoing.Writer.TryComplete();
            await writing;
        }
    }

    private static async Task WriteAsync(StreamWriter writer, ChannelReader<Envelope> outgoing, CancellationToken token)
    {
        try
        {
            await foreach (var envelope in outgoing.ReadAllAsync(token))
            {
                await writer.WriteLineAsync(Wire.Write(envelope).AsMemory(), token);
            }
        }
        catch (Exception closed) when (closed is OperationCanceledException or IOException or ObjectDisposedException)
        {
        }
    }

    private async Task HandleAsync(Envelope envelope, ChannelWriter<Envelope> replies, Dictionary<string, Action<Envelope>> watching, CancellationToken token)
    {
        if (envelope.Chat is not { } id)
        {
            return;
        }

        if (envelope.Op == Op.Open)
        {
            var opened = await OpenAsync(envelope, token);

            if (opened is null)
            {
                replies.TryWrite(new Envelope(Op.Opened, id, envelope.Number, Error: $"There is no hired agent '{envelope.Agent}' to open this chat with."));

                return;
            }

            if (!watching.ContainsKey(id))
            {
                void Forward(Envelope said) => replies.TryWrite(said);
                watching[id] = Forward;
                opened.Said += Forward;
                opened.Watch(+1);
            }

            replies.TryWrite(opened.Opened(envelope.Number));

            return;
        }

        if (!_chats.TryGetValue(id, out var chat))
        {
            return;
        }

        switch (envelope.Op)
        {
            case Op.Ask when envelope.Prompt is { } prompt:
                chat.Ask(prompt, envelope.Setup ?? Setup.Default);
                break;
            case Op.Cancel:
                chat.Cancel();
                break;
            case Op.Withdraw when envelope.Prompt is { } withdrawn:
                chat.Withdraw(withdrawn);
                break;
            case Op.Decide when envelope is { Request: { } request, Decision: { } decision }:
                chat.Decide(request, decision);
                break;
            case Op.Rebrief when envelope.Brief is { } brief:
                chat.Session.Rebrief(brief);
                break;
            case Op.Stop when envelope.Job is { } job:
                chat.Session.Stop(job);
                break;
            case Op.Output when envelope.Job is { } job:
                replies.TryWrite(new Envelope(Op.Answer, id, envelope.Number, Output: chat.Session.Output(job)));
                break;
            case Op.Leave:
                Unwatch(id, watching);
                break;
            case Op.End:
                Unwatch(id, watching);
                End(chat);
                break;
        }
    }

    private async Task<HostedChat?> OpenAsync(Envelope envelope, CancellationToken token)
    {
        var id = envelope.Chat!;

        await _opening.WaitAsync(token);

        try
        {
            if (_chats.TryGetValue(id, out var open))
            {
                open.Opener = envelope.Opener ?? open.Opener;

                if (envelope.Brief is { } brief)
                {
                    open.Session.Rebrief(brief);
                }

                return open;
            }

            if (envelope.Brief is not { } briefed || crew.Find(envelope.Agent ?? string.Empty) is not { IsSuccess: true } agent)
            {
                return null;
            }

            var keep = envelope.Keep ?? true;
            var kept = keep ? await transcripts.LoadAsync(id, token) : null;
            var turns = kept?.Settled() ?? envelope.Turns ?? [];
            var session = agent.Value.Begin(briefed, kept?.Session ?? envelope.Session);
            var hosted = new HostedChat(id, session, turns, keep, transcripts, ringer, Path.GetFileName(briefed.Folder.TrimEnd('/')), agent.Value.Color, launch ?? Launcher.Start)
            {
                Opener = envelope.Opener,
            };
            _chats[id] = hosted;
            LogOpened(id, keep);

            if (keep && (kept is null || kept.Pending.Count > 0))
            {
                await hosted.SaveAsync();
            }

            return hosted;
        }
        finally
        {
            _opening.Release();
        }
    }

    private void Unwatch(string id, Dictionary<string, Action<Envelope>> watching)
    {
        if (!watching.Remove(id, out var forward) || !_chats.TryGetValue(id, out var chat))
        {
            return;
        }

        chat.Said -= forward;
        chat.Watch(-1);

        if (!chat.Keep && chat.Watchers <= 0)
        {
            End(chat);
        }
    }

    private void End(HostedChat chat)
    {
        if (!_chats.TryRemove(chat.Id, out _))
        {
            return;
        }

        chat.End();
        transcripts.Forget(chat.Id);
        LogEnded(chat.Id);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Allay listening on {Path}")]
    private partial void LogListening(string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Opened chat {Chat} (kept: {Keep})")]
    private partial void LogOpened(string chat, bool keep);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read a message: {Problem}")]
    private partial void LogUnreadable(string? problem);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ended chat {Chat}")]
    private partial void LogEnded(string chat);
}
