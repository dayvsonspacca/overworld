using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Artworks;

[Operation("show", "Shows one version of a painting with its HTML, found by its id, or the one painted last; the latest version unless one is asked for.")]
public sealed class ShowArtworkOp(ArtworkStore store) : IOperation<ShowArtwork, Canvas>
{
    public const string Empty = "painting.none";

    public async Task<Result<Canvas>> ExecuteAsync(ShowArtwork input, CancellationToken cancellationToken)
    {
        var found = input.Id is { } id ? await store.FindAsync(id, cancellationToken) : await LastAsync(cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var artwork = found.Value;

        if (input.Version is not { } number)
        {
            return Canvas.Latest(artwork, withHtml: true);
        }

        return artwork.Version(number) is { } version
            ? Canvas.Of(artwork, version, withHtml: true)
            : NoVersion(artwork, number);
    }

    public static Error NoVersion(Artwork artwork, int number) =>
        new("painting.version_not_found", $"'{artwork.Title}' has versions 1 to {artwork.Versions.Count}, not {number}.");

    private async Task<Result<Artwork>> LastAsync(CancellationToken cancellationToken)
    {
        var artworks = await store.ListAsync(cancellationToken);

        if (!artworks.IsSuccess)
        {
            return artworks.Error;
        }

        return artworks.Value.Count > 0 ? artworks.Value[0] : new Error(Empty, "Nothing has been painted yet.");
    }
}
