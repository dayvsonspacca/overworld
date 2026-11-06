using System.Text.Json.Serialization;

namespace Overworld.Apps.Crafter.Projects;

public sealed record ProjectSettings(
    string Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Icon = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Axioms = null);
