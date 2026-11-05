using Overworld.Lifetime;

namespace Overworld.Spawner;

internal sealed class QuickshellNotInstalledException(Exception inner)
    : OverworldException("Could not start Quickshell ('qs'). Is it installed?", exitCode: 127, inner);
