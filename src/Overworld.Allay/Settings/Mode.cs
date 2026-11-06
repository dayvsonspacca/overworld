using System.Text.Json.Serialization;

namespace Overworld.Allay.Settings;

[JsonConverter(typeof(JsonStringEnumConverter<Mode>))]
public enum Mode
{
    Auto,
    Edits,
    Plan,
}
