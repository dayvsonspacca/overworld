namespace Overworld.Daemon.Lifetime;

internal abstract class OverworldException(string message, int exitCode, Exception? inner = null)
    : Exception(message, inner)
{
    public int ExitCode { get; } = exitCode;
}
