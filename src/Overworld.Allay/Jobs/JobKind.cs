using System.Text.Json.Serialization;

namespace Overworld.Allay.Jobs;

[JsonConverter(typeof(JsonStringEnumConverter<JobKind>))]
public enum JobKind
{
    Command,
    Monitor,
    Agent,
}
