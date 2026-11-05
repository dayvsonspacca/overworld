using System.Runtime.InteropServices;

namespace Overworld.Daemon.Spawner;

internal static partial class LibC
{
    public const int SIGTERM = 15;

    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    public static partial int Kill(int pid, int signal);
}
