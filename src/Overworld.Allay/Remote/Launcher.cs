using System.Diagnostics;

namespace Overworld.Allay.Remote;

public static class Launcher
{
    public static void Start(IReadOnlyList<string> command)
    {
        var start = new ProcessStartInfo("sh") { UseShellExecute = false };

        foreach (var argument in (string[])["-c", "\"$@\" >/dev/null 2>&1 &", "sh", .. command])
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start);
        process?.WaitForExit();
    }
}
