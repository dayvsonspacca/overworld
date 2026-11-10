using System.Text.Json.Serialization;

namespace Overworld.Apps.Crafter.Conversations;

internal sealed record ChatEntry(
    Guid Id,
    string Agent,
    string Title,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SessionId = null,
    IReadOnlyList<TurnEntry>? Turns = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? Touched = null);
