using Overworld.Paths;

namespace Overworld.Shell;

internal sealed class ShellLocator : IShellLocator
{
    private const string EntryPoint = "shell.qml";

    public string Find()
    {
        var directory = Environment.GetEnvironmentVariable("OVERWORLD_SHELL")
            ?? Path.Combine(XdgDirectories.ConfigHome(), "quickshell", "overworld");

        return File.Exists(Path.Combine(directory, EntryPoint))
            ? directory
            : throw new ShellNotFoundException(directory);
    }
}
