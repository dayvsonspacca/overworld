using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Speech), "speech")]
[JsonDerivedType(typeof(Thought), "thought")]
[JsonDerivedType(typeof(Plan), "plan")]
[JsonDerivedType(typeof(Act), "act")]
[JsonDerivedType(typeof(ActResult), "actResult")]
[JsonDerivedType(typeof(Notice), "notice")]
[JsonDerivedType(typeof(Permission), "permission")]
[JsonDerivedType(typeof(Questions), "questions")]
[JsonDerivedType(typeof(Withdrawn), "withdrawn")]
[JsonDerivedType(typeof(Finished), "finished")]
[JsonDerivedType(typeof(Stopped), "stopped")]
[JsonDerivedType(typeof(CutShort), "cutShort")]
[JsonDerivedType(typeof(Refused), "refused")]
[JsonDerivedType(typeof(Failed), "failed")]
public abstract record Moment;
