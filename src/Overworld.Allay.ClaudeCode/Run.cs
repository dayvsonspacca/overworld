using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Overworld.Allay.ClaudeCode.Cli;
using Overworld.Allay.ClaudeCode.Protocol;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;

namespace Overworld.Allay.ClaudeCode;

[SuppressMessage(
    "Design",
    "CA1001",
    Justification = "The writing gate never hands out its wait handle, so it holds nothing that needs disposing.")]
internal sealed class Run
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(3);

    private readonly Process _process;

    private readonly Transcript _transcript;

    private readonly Task<string> _complaints;

    private readonly SemaphoreSlim _writing = new(1, 1);

    private readonly Lock _gate = new();

    private readonly List<Turn> _pending = [];

    private Turn? _current;

    private bool _tracksLifecycle;

    private bool _closed;

    private int _requests;

    private Run(Process process, Options options, string folder)
    {
        _process = process;
        Options = options;
        _transcript = new Transcript(folder);
        _complaints = process.StandardError.ReadToEndAsync(CancellationToken.None);
    }

    public event Action<string>? Known;

    public event Action<IReadOnlyList<Job>>? JobsChanged;

    public event Action<Turn>? Woke;

    public Options Options { get; }

    public Task Reading { get; private set; } = Task.CompletedTask;

    public IReadOnlyList<Job> Jobs
    {
        get
        {
            lock (_gate)
            {
                return [.. _transcript.Jobs];
            }
        }
    }

    public bool IsOpen
    {
        get
        {
            lock (_gate)
            {
                return !_closed;
            }
        }
    }

    public static Run? Start(Options options, string folder, IEnumerable<string> arguments) =>
        Launcher.Start(options.Executable, folder, arguments) is { } process ? new Run(process, options, folder) : null;

    public string? OutputOf(string job)
    {
        lock (_gate)
        {
            return _transcript.OutputOf(job);
        }
    }

    public void Begin() => Reading = Task.Run(ReadAsync);

    public async Task FinishAsync()
    {
        if (await Task.WhenAny(Reading, Task.Delay(Patience)) != Reading)
        {
            Kill();
            await Reading;
        }
    }

    public Turn Ask(string text, IReadOnlyList<Picture> pictures)
    {
        var turn = new Turn(Guid.NewGuid().ToString());

        lock (_gate)
        {
            _pending.Add(turn);
        }

        _ = SendAsync(Messages.Asking(text, pictures, turn.Id));

        return turn;
    }

    public string? Respond(string request, Decision decision)
    {
        string input;
        string? suggestions;

        lock (_gate)
        {
            input = _transcript.InputOf(request);
            suggestions = _transcript.SuggestionsOf(request);
        }

        _ = SendAsync(Messages.Answering(request, decision, input, suggestions));

        return suggestions;
    }

    public void Stop(string job) => _ = SendAsync(Messages.Stopping(Request(), job));

    public async Task HaltAsync(Turn turn)
    {
        lock (_gate)
        {
            _pending.Remove(turn);
        }

        if (Jobs.Count > 0)
        {
            await SendAsync(Messages.Interrupting(Request()));

            if (await Task.WhenAny(turn.Done, Task.Delay(Patience)) == turn.Done)
            {
                return;
            }
        }

        Kill();
    }

    public void Kill() => Launcher.Kill(_process);

    private string Request() => $"overworld-{Interlocked.Increment(ref _requests)}";

    private async Task ReadAsync()
    {
        while (await LineAsync() is { } line)
        {
            var (moved, jobs, woken) = Hear(line);

            if (woken is not null)
            {
                Woke?.Invoke(woken);
            }

            if (moved)
            {
                JobsChanged?.Invoke(jobs);
            }
        }

        await EndedAsync();
    }

    private (bool Moved, IReadOnlyList<Job> Jobs, Turn? Woken) Hear(string line)
    {
        Turn? woken = null;

        lock (_gate)
        {
            var moments = _transcript.Hear(line);

            if (_transcript.SessionId is { } session)
            {
                Known?.Invoke(session);
            }

            if (_transcript.Started is { } started)
            {
                _tracksLifecycle = true;
                _current = _pending.Find(turn => turn.Id == started) ?? new Turn(started);
                _pending.Remove(_current);
            }

            if (_current is null && (_transcript.Opened || moments.Count > 0))
            {
                if (!_tracksLifecycle && _pending.Count > 0)
                {
                    _current = _pending[0];
                    _pending.RemoveAt(0);
                }
                else if (_transcript.Opened || moments.Any(moment => moment is not Outcome))
                {
                    _current = woken = new Turn(Guid.NewGuid().ToString());
                }
            }

            foreach (var moment in moments)
            {
                if (_current is not { } turn)
                {
                    continue;
                }

                turn.Hear(moment);

                if (moment is Outcome)
                {
                    _current = null;
                    Settle();
                }
            }

            return (_transcript.JobsMoved, [.. _transcript.Jobs], woken);
        }
    }

    private void Settle()
    {
        if (_transcript.Jobs.Count == 0 && _pending.Count == 0 && !_closed)
        {
            _closed = true;
            _ = CloseAsync();
        }
    }

    private async Task EndedAsync()
    {
        await _process.WaitForExitAsync(CancellationToken.None);
        var complaint = (await _complaints).Trim();
        var reason = complaint.Length > 0 ? complaint : $"Claude Code exited with code {_process.ExitCode} without a reply.";

        bool hadJobs;
        List<Turn> unfinished;

        lock (_gate)
        {
            _closed = true;
            hadJobs = _transcript.Jobs.Count > 0;
            unfinished = [.. _pending];

            if (_current is { } turn)
            {
                unfinished.Insert(0, turn);
            }

            _pending.Clear();
            _current = null;
        }

        foreach (var turn in unfinished)
        {
            turn.Hear(new Failed(reason));
        }

        if (hadJobs)
        {
            JobsChanged?.Invoke([]);
        }

        _process.Dispose();
    }

    private async Task<string?> LineAsync()
    {
        try
        {
            return await _process.StandardOutput.ReadLineAsync();
        }
        catch (Exception closed) when (closed is IOException or ObjectDisposedException)
        {
            return null;
        }
    }

    private async Task SendAsync(string line)
    {
        await _writing.WaitAsync();

        try
        {
            await _process.StandardInput.WriteLineAsync(line);
            await _process.StandardInput.FlushAsync();
        }
        catch (Exception closed) when (closed is IOException or ObjectDisposedException or InvalidOperationException)
        {
        }
        finally
        {
            _writing.Release();
        }
    }

    private async Task CloseAsync()
    {
        await _writing.WaitAsync();

        try
        {
            _process.StandardInput.Close();
        }
        catch (Exception closed) when (closed is IOException or ObjectDisposedException or InvalidOperationException)
        {
        }
        finally
        {
            _writing.Release();
        }
    }

    internal sealed class Turn(string id)
    {
        private readonly Channel<Moment> _moments = Channel.CreateUnbounded<Moment>(new UnboundedChannelOptions { SingleReader = true });

        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Id { get; } = id;

        public Task Done => _done.Task;

        public ChannelReader<Moment> Moments => _moments.Reader;

        public void Hear(Moment moment)
        {
            if (!_moments.Writer.TryWrite(moment) || moment is not Outcome)
            {
                return;
            }

            _moments.Writer.TryComplete();
            _done.TrySetResult();
        }
    }
}
