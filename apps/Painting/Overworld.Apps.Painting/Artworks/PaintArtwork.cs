using System.ComponentModel;

namespace Overworld.Apps.Painting.Artworks;

public sealed record PaintArtwork(
    [property: Description("What the painting shows, in the window's title bar. Needed for a new painting; leave it out to keep a painting's title.")]
    string? Title = null,
    [property: Description("The page to show, as HTML. Give this or a file.")]
    string? Html = null,
    [property: Description("An HTML file whose contents become the page to show. Give this or html.")]
    string? File = null,
    [property: Description("The id of a painting to paint again, as paint gave it: the page becomes its next version and the earlier ones are kept. Leave it out to paint a new one.")]
    Guid? Id = null,
    [property: Description("The group the painting belongs to: Design for the design of a screen, a component or an app. Leave it out to keep a painting's group.")]
    ArtworkGroup? Group = null);
