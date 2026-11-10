using System.ComponentModel;

namespace Overworld.Apps.Painting.Artworks;

public sealed record RestoreArtwork(
    [property: Description("The id of the painting, as paint gave it.")]
    Guid Id,
    [property: Description("The version to go back to, counting from 1. It is painted again as the newest version; none is lost.")]
    int Version);
