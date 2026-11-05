namespace Overworld.Redstone;

public sealed class AlreadyRunningException(string path)
    : Exception($"overworld is already running (socket {path} is answering)");
