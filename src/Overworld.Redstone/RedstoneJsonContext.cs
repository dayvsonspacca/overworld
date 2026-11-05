using System.Text.Json.Serialization;

namespace Overworld.Redstone;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectRequiredConstructorParameters = true,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Response))]
internal sealed partial class RedstoneJsonContext : JsonSerializerContext;
