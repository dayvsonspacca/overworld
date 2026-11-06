using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Picture), "picture")]
[JsonDerivedType(typeof(FileMention), "file")]
public abstract record Attachment;
