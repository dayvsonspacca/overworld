using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AllowOnce), "allowOnce")]
[JsonDerivedType(typeof(AllowAlways), "allowAlways")]
[JsonDerivedType(typeof(Deny), "deny")]
[JsonDerivedType(typeof(Answers), "answers")]
[JsonDerivedType(typeof(Skip), "skip")]
public abstract record Decision;
