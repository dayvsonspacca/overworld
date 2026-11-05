namespace Overworld.Daemon.Spawner;

internal sealed class ProcessStartException(string fileName, Exception inner)
    : Exception($"Could not start '{fileName}'.", inner);
