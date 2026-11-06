using System.Text.Json.Serialization;

namespace Overworld.Apps.Crafter.Preferences;

public sealed record UserPreferences(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProjectsRoot = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Library = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? DefaultSkills = null);
