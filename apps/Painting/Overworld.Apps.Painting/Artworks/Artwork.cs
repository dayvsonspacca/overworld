using System.Text.Json.Serialization;

namespace Overworld.Apps.Painting.Artworks;

public sealed record Artwork(Guid Id, string Title, ArtworkGroup? Group, IReadOnlyList<ArtworkVersion> Versions)
{
    [JsonIgnore]
    public ArtworkVersion Latest => Versions[^1];

    public static Artwork New(string title, ArtworkGroup? group, string html, DateTimeOffset painted) =>
        new(Guid.NewGuid(), title, group, [new ArtworkVersion(1, html, painted)]);

    public ArtworkVersion? Version(int number) => Versions.FirstOrDefault(version => version.Number == number);

    public Artwork Painted(string html, DateTimeOffset painted) =>
        this with { Versions = [.. Versions, new ArtworkVersion(Versions.Count + 1, html, painted)] };
}
