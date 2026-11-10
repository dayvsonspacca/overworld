using System.ComponentModel;

namespace Overworld.Apps.Painting.Artworks;

public sealed record GroupArtwork(
    [property: Description("The id of the painting, as paint gave it.")]
    Guid Id,
    [property: Description("The group to put the painting in: Design for the design of a screen, a component or an app. Leave it out to take it out of its group.")]
    ArtworkGroup? Group = null);
