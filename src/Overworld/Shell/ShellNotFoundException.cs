using Overworld.Lifetime;

namespace Overworld.Shell;

internal sealed class ShellNotFoundException(string directory)
    : OverworldException($"shell.qml not found in {directory} (set OVERWORLD_SHELL or link the shell there)", exitCode: 1)
{
    public string Directory { get; } = directory;
}
