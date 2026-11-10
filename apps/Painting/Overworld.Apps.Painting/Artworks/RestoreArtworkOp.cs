using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Artworks;

[Operation("restore", "Goes back to an earlier version of a painting by painting it again as the newest version, so no version is lost, and signals Painting to show it now.")]
public sealed class RestoreArtworkOp(ArtworkStore store, BeaconClient beacon, TimeProvider time) : IOperation<RestoreArtwork, Canvas>
{
    public async Task<Result<Canvas>> ExecuteAsync(RestoreArtwork input, CancellationToken cancellationToken)
    {
        var found = await store.FindAsync(input.Id, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        if (found.Value.Version(input.Version) is not { } version)
        {
            return ShowArtworkOp.NoVersion(found.Value, input.Version);
        }

        var restored = await store.ChangeAsync(input.Id, artwork => artwork.Painted(version.Html, time.GetUtcNow()), cancellationToken);

        if (!restored.IsSuccess)
        {
            return restored.Error;
        }

        await beacon.LightAsync("painting", cancellationToken);

        return Canvas.Latest(restored.Value);
    }
}
