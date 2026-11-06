using System.Text.Json.Serialization;

namespace Overworld.Allay.Settings;

public sealed record Setup(
    Mode Mode = Mode.Auto,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Model = null,
    Effort Effort = Effort.Default)
{
    public static Setup Default { get; } = new();
}
