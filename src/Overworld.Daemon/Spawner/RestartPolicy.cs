namespace Overworld.Daemon.Spawner;

internal sealed class RestartPolicy
{
    private const int MaxRestarts = 3;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);

    private readonly Queue<DateTimeOffset> _crashes = new();

    public bool CanRestart => _crashes.Count <= MaxRestarts;

    public void RecordCrash()
    {
        var now = DateTimeOffset.UtcNow;
        _crashes.Enqueue(now);

        while (now - _crashes.Peek() > Window)
            _crashes.Dequeue();
    }
}
