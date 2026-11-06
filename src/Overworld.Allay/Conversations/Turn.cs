using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Prompt), "prompt")]
[JsonDerivedType(typeof(Reply), "reply")]
public abstract record Turn;
