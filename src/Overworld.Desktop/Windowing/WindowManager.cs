namespace Overworld.Desktop.Windowing;

public sealed record WindowManager(bool Tiles)
{
    public static WindowManager Detect() => Detect(Environment.GetEnvironmentVariable);

    internal static WindowManager Detect(Func<string, string?> variable) =>
        new(!string.IsNullOrEmpty(variable("HYPRLAND_INSTANCE_SIGNATURE")));
}
