using System.Text.Json.Serialization;

namespace Overworld.Apps.Crafter.Projects;

public sealed record Shelved(
    string Folder,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Skills = null);
