using Overworld.Daemon.Lifetime;

namespace Overworld.Daemon.Spawner;

internal sealed class QuickshellNotInstalledException(Exception inner)
    : OverworldException("Could not start Quickshell ('qs'). Is it installed?", exitCode: 127, inner);
