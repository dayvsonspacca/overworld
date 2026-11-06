using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonConverter(typeof(JsonStringEnumConverter<NoticeKind>))]
public enum NoticeKind
{
    Other,
    Model,
    Limit,
    Context,
}
