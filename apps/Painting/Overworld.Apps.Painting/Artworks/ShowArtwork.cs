using System.ComponentModel;

namespace Overworld.Apps.Painting.Artworks;

public sealed record ShowArtwork(
    [property: Description("The id of the painting, as paint gave it. Leave it out for the one painted last.")]
    Guid? Id = null,
    [property: Description("Which version to show, counting from 1. Leave it out for the latest.")]
    int? Version = null);
