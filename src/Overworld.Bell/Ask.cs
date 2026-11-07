using System.Text.Json.Serialization;

namespace Overworld.Bell;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Questions), "questions")]
[JsonDerivedType(typeof(Buttons), "buttons")]
public abstract record Ask;

public sealed record Questions(IReadOnlyList<Question> Asked) : Ask;

public sealed record Buttons(string? Subject, IReadOnlyList<Button> Offered) : Ask;
