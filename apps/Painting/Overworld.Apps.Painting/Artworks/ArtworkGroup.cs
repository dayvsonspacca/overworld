using System.Text.Json.Serialization;

namespace Overworld.Apps.Painting.Artworks;

[JsonConverter(typeof(JsonStringEnumConverter<ArtworkGroup>))]
public enum ArtworkGroup
{
    Design,
}
