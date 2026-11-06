using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Finished), "finished")]
[JsonDerivedType(typeof(Stopped), "stopped")]
[JsonDerivedType(typeof(CutShort), "cutShort")]
[JsonDerivedType(typeof(Refused), "refused")]
[JsonDerivedType(typeof(Failed), "failed")]
public abstract record Outcome : Moment;
