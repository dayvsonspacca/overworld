namespace Overworld.Chest;

public static class DataDirectory
{
    public static string For(string name) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "overworld",
        name);
}
