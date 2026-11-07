using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Artworks;

[Operation("show", "Shows one painting with its HTML, found by its id, or the one painted last.")]
public sealed class ShowArtworkOp(ArtworkStore store) : IOperation<ShowArtwork, Artwork>
{
    public async Task<Result<Artwork>> ExecuteAsync(ShowArtwork input, CancellationToken cancellationToken)
    {
        var artworks = await store.ListAsync(cancellationToken);

        if (!artworks.IsSuccess)
        {
            return artworks.Error;
        }

        if (input.Id is { } id)
        {
            return artworks.Value.FirstOrDefault(artwork => artwork.Id == id) is { } found ? found : ArtworkStore.NotFound(id);
        }

        return artworks.Value.MaxBy(artwork => artwork.Painted) is { } last
            ? last
            : new Error(Empty, "Nothing has been painted yet.");
    }

    public const string Empty = "painting.none";
}
