namespace Overworld.Redstone;

public static class SocketPath
{
    private const string FileName = "overworld.sock";

    public static string Resolve()
    {
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        var directory = Path.IsPathRooted(runtime) ? runtime : Path.GetTempPath();

        return Path.Combine(directory, FileName);
    }
}
