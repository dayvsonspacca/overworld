using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonConverter(typeof(JsonStringEnumConverter<Reach>))]
public enum Reach
{
    Conversation,
    Folder,
    Everywhere,
}
