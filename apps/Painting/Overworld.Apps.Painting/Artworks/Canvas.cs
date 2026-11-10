namespace Overworld.Apps.Painting.Artworks;

public sealed record Canvas(Guid Id, string Title, ArtworkGroup? Group, int Version, int Versions, DateTimeOffset Painted, string? Html = null)
{
    public static Canvas Of(Artwork artwork, ArtworkVersion version, bool withHtml = false) =>
        new(artwork.Id, artwork.Title, artwork.Group, version.Number, artwork.Versions.Count, version.Painted, withHtml ? version.Html : null);

    public static Canvas Latest(Artwork artwork, bool withHtml = false) => Of(artwork, artwork.Latest, withHtml);
}
