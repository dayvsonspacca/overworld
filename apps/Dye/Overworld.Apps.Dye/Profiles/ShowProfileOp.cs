using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Profiles;

[Operation("profile-show", "Shows the profile the shell wears: the account's name and its picture, if it has one.")]
public sealed class ShowProfileOp(ProfileStore store) : IOperation<ShowProfile, Profile>
{
    public Task<Result<Profile>> ExecuteAsync(ShowProfile input, CancellationToken cancellationToken) =>
        Task.FromResult<Result<Profile>>(store.Read());
}
