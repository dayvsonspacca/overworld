using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Artworks;

[Operation("list", "Lists the paintings, the one painted last first, each with its latest version and how many versions it has, without their HTML.")]
public sealed class ListArtworksOp(ArtworkStore store) : IOperation<ListArtworks, IReadOnlyList<Canvas>>
{
    public async Task<Result<IReadOnlyList<Canvas>>> ExecuteAsync(ListArtworks input, CancellationToken cancellationToken)
    {
        var artworks = await store.ListAsync(cancellationToken);

        if (!artworks.IsSuccess)
        {
            return artworks.Error;
        }

        return artworks.Value
            .Where(artwork => input.Group is null || artwork.Group == input.Group)
            .Select(artwork => Canvas.Latest(artwork))
            .ToList();
    }
}
