using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Artworks;

[Operation("group", "Puts a painting in a group, or takes it out of its group, without painting a new version, and signals Painting to show it now.")]
public sealed class GroupArtworkOp(ArtworkStore store, BeaconClient beacon) : IOperation<GroupArtwork, Canvas>
{
    public async Task<Result<Canvas>> ExecuteAsync(GroupArtwork input, CancellationToken cancellationToken)
    {
        var grouped = await store.ChangeAsync(input.Id, artwork => artwork with { Group = input.Group }, cancellationToken);

        if (!grouped.IsSuccess)
        {
            return grouped.Error;
        }

        await beacon.LightAsync("painting", cancellationToken);

        return Canvas.Latest(grouped.Value);
    }
}
