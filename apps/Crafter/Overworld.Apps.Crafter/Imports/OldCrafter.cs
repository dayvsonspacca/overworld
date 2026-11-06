namespace Overworld.Apps.Crafter.Imports;

public sealed record OldCrafter(string Config, string Data)
{
    public static OldCrafter Here() => new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "crafter"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "crafter"));
}
