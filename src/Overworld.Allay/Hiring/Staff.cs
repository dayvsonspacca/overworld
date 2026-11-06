using System.Text.Json.Serialization;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Hiring;

public sealed record Staff(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Hired = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, Setup>? Setups = null);
