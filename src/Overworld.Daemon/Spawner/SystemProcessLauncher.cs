using System.ComponentModel;
using System.Diagnostics;

namespace Overworld.Daemon.Spawner;

internal sealed class SystemProcessLauncher : IProcessLauncher
{
    public IManagedProcess Start(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment)
    {
        var info = new ProcessStartInfo(fileName);
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        foreach (var (name, value) in environment)
            info.Environment[name] = value;

        var process = new Process { StartInfo = info };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            process.Dispose();
            throw new ProcessStartException(fileName, exception);
        }

        return new SystemProcess(process);
    }
}
