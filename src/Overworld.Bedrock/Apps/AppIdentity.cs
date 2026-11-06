namespace Overworld.Bedrock.Apps;

public sealed record AppIdentity(string Name)
{
    public string Command => $"overworld:{Name}";

    public string Title => char.ToUpperInvariant(Name[0]) + Name[1..];
}
