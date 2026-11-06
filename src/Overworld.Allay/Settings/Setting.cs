using System.Text.Json.Serialization;

namespace Overworld.Allay.Settings;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Choice), "choice")]
[JsonDerivedType(typeof(Entry), "entry")]
public abstract record Setting(string Key, string Title, string About);
