namespace Overworld.Bedrock.Apps;

public sealed record AppIdentity(string Name)
{
    public string Command => $"overworld:{Name}";
}
