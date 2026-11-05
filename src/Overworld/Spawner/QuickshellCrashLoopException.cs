using Overworld.Lifetime;

namespace Overworld.Spawner;

internal sealed class QuickshellCrashLoopException(int lastExitCode)
    : OverworldException($"Quickshell crashed too many times (last exit code {lastExitCode}), giving up", exitCode: 2)
{
    public int LastExitCode { get; } = lastExitCode;
}
