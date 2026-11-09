using System.Collections.Concurrent;

namespace Overworld.Web.Mirroring;

public sealed class Loop : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];

    private readonly Thread _thread;

    public Loop()
    {
        _thread = new Thread(Run) { Name = "Overworld UI", IsBackground = true };
        _thread.Start();
    }

    public bool IsCurrent => Thread.CurrentThread == _thread;

    public override void Post(SendOrPostCallback d, object? state)
    {
        if (!_queue.IsAddingCompleted)
        {
            _queue.Add((d, state));
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (IsCurrent)
        {
            d(state);

            return;
        }

        using var done = new ManualResetEventSlim();
        Exception? failed = null;

        Post(
            _ =>
            {
                try
                {
                    d(state);
                }
                catch (Exception exception)
                {
                    failed = exception;
                }
                finally
                {
                    done.Set();
                }
            },
            null);

        done.Wait();

        if (failed is not null)
        {
            throw new InvalidOperationException("What was sent to the loop failed.", failed);
        }
    }

    public void Post(Action action) => Post(_ => action(), null);

    public Task<T> RunAsync<T>(Func<T> work)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        Post(() =>
        {
            try
            {
                done.SetResult(work());
            }
            catch (Exception exception)
            {
                done.SetException(exception);
            }
        });

        return done.Task;
    }

    public override SynchronizationContext CreateCopy() => this;

    public void Dispose()
    {
        _queue.CompleteAdding();
        _queue.Dispose();
    }

    private void Run()
    {
        SetSynchronizationContext(this);

        try
        {
            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
            {
                try
                {
                    callback(state);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"overworld: {exception}");
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
