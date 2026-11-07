using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Profiles;

[Operation("profile-picture-remove", "Removes the profile's picture, so the shell shows the account's initial, and signals the shell to show it now.")]
public sealed class RemovePictureOp(ProfileStore store, BeaconClient beacon) : IOperation<RemovePicture, Profile>
{
    public async Task<Result<Profile>> ExecuteAsync(RemovePicture input, CancellationToken cancellationToken)
    {
        var removed = store.RemovePicture();
        await beacon.LightAsync("profile", cancellationToken);

        return removed;
    }
}
