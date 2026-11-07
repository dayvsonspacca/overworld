using System.ComponentModel;

namespace Overworld.Apps.Painting.Artworks;

public sealed record PaintArtwork(
    [property: Description("What the painting shows, in the window's title bar. Needed for a new painting; leave it out to keep a painting's title.")]
    string? Title = null,
    [property: Description("The page to show, as HTML. Give this or a file.")]
    string? Html = null,
    [property: Description("An HTML file whose contents become the page to show. Give this or html.")]
    string? File = null,
    [property: Description("The id of a painting to change, as paint gave it. Leave it out to paint a new one.")]
    Guid? Id = null);
