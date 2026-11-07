using System.Text.Json.Serialization;

namespace Overworld.Bell;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Answered), "answered")]
[JsonDerivedType(typeof(Pressed), "pressed")]
[JsonDerivedType(typeof(Skipped), "skipped")]
public abstract record Reply;

public sealed record Answered(IReadOnlyList<Picked> Given) : Reply;

public sealed record Pressed(string Key) : Reply;

public sealed record Skipped : Reply;

public sealed record Picked(IReadOnlyList<string> Labels, string? Written = null);
