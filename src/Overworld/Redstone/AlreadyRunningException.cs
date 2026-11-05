using Overworld.Lifetime;

namespace Overworld.Redstone;

internal sealed class AlreadyRunningException(string path)
    : OverworldException($"overworld is already running (socket {path} is answering)", exitCode: 3);
