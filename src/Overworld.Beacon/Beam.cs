using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Overworld.Beacon;

public sealed class Beam
{
    private const int Backlog = 16;

    private readonly Lock _lock = new();
    private readonly List<ChannelWriter<string>> _watchers = [];

    public void Light(string signal)
    {
        lock (_lock)
        {
            foreach (var watcher in _watchers)
            {
                watcher.TryWrite(signal);
            }
        }
    }

    public async IAsyncEnumerable<string> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(Backlog)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

        lock (_lock)
        {
            _watchers.Add(channel.Writer);
        }

        try
        {
            await foreach (var signal in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return signal;
            }
        }
        finally
        {
            lock (_lock)
            {
                _watchers.Remove(channel.Writer);
            }
        }
    }
}
