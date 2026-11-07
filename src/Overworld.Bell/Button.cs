using System.Text.Json.Serialization;

namespace Overworld.Bell;

public sealed record Button(string Key, string Label, Tone Tone = Tone.Plain);

[JsonConverter(typeof(JsonStringEnumConverter<Tone>))]
public enum Tone
{
    Plain,
    Primary,
    Danger,
}
