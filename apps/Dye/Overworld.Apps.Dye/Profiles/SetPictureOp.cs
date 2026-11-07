using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Profiles;

[Operation("profile-picture-set", "Makes a picture the profile's, shown in the start menu and on the lock screen, and signals the shell to show it now.")]
public sealed class SetPictureOp(ProfileStore store, BeaconClient beacon) : IOperation<SetPicture, Profile>
{
    public async Task<Result<Profile>> ExecuteAsync(SetPicture input, CancellationToken cancellationToken)
    {
        var set = await store.SetPictureAsync(input.File.Trim(), cancellationToken);

        if (set.IsSuccess)
        {
            await beacon.LightAsync("profile", cancellationToken);
        }

        return set;
    }
}
