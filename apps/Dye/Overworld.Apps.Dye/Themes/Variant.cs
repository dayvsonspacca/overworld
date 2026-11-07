using System.Text.Json.Serialization;

namespace Overworld.Apps.Dye.Themes;

[JsonConverter(typeof(JsonStringEnumConverter<Variant>))]
public enum Variant
{
    Dark,
    Light,
}
