namespace Overworld.Apps.Crafter.Conversations;

public sealed record ChatId(Guid Value)
{
    public static ChatId New() => new(Guid.NewGuid());
}
