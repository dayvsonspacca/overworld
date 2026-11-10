using Overworld.Redstone;

namespace Overworld.Allay.Remote;

public static class AllaySocket
{
    public const string Name = "overworld-allay";

    public static string Path => SocketPath.Resolve(Name);
}
