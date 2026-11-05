namespace Overworld.Spawner;

internal interface IProcessLauncher
{
    IManagedProcess Start(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment);
}
