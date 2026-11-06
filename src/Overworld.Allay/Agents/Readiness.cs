using System.Text.Json.Serialization;

namespace Overworld.Allay.Agents;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Ready), "ready")]
[JsonDerivedType(typeof(Blocked), "blocked")]
public abstract record Readiness(string State, string Detail);
