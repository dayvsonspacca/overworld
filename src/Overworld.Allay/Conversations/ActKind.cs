using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonConverter(typeof(JsonStringEnumConverter<ActKind>))]
public enum ActKind
{
    Other,
    Command,
    Read,
    Change,
    Create,
    Search,
    Browse,
    Delegate,
    Tool,
}
