using System.ComponentModel;
using System.Diagnostics;

namespace Overworld.Spawner;

internal sealed class SystemProcessLauncher : IProcessLauncher
{
    public IManagedProcess Start(string fileName, IReadOnlyList<string> arguments)
    {
        try
        {
            return new SystemProcess(Process.Start(fileName, arguments));
        }
        catch (Win32Exception exception)
        {
            throw new ProcessStartException(fileName, exception);
        }
    }
}
