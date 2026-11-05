namespace Overworld.Bedrock.Apps;

public sealed record AppIdentity(string Name)
{
    public string Command => $"overworld:{Name}";

    public string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "overworld",
        Name);
}
