using System.Text.Json.Serialization;

namespace Overworld.Allay.Settings;

[JsonConverter(typeof(JsonStringEnumConverter<Effort>))]
public enum Effort
{
    Default,
    Max,
}
