namespace Overworld.Redstone;

public static class SocketPath
{
    public static string Resolve(string name = "overworld")
    {
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        var directory = Path.IsPathRooted(runtime) ? runtime : Path.GetTempPath();

        return Path.Combine(directory, $"{name}.sock");
    }
}
