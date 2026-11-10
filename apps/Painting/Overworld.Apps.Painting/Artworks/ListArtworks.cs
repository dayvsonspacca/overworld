using System.ComponentModel;

namespace Overworld.Apps.Painting.Artworks;

public sealed record ListArtworks(
    [property: Description("Only the paintings of this group, like Design. Leave it out for every painting.")]
    ArtworkGroup? Group = null);
