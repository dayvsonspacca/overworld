using Overworld.Apps.Dye.Presentation.Profiles;
using Overworld.Apps.Dye.Profiles;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Presentation.Tests.Profiles;

public sealed class ProfileViewModelTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Operation("profile-answer", "Answers with the same profile every time.")]
    private sealed class AnswerOp<TInput>(Result<Profile> answer) : IOperation<TInput, Profile>
        where TInput : notnull
    {
        public Task<Result<Profile>> ExecuteAsync(TInput input, CancellationToken cancellationToken) => Task.FromResult(answer);
    }

    private static Operation<TInput, Profile> Answer<TInput>(Result<Profile> answer)
        where TInput : notnull => Operation.From(new AnswerOp<TInput>(answer));

    [Fact]
    public async Task ShowsTheInitialWithoutAPicture()
    {
        var profile = new ProfileViewModel(
            Answer<ShowProfile>(new Profile("steve", null)),
            Answer<SetPicture>(new Profile("steve", null)),
            Answer<RemovePicture>(new Profile("steve", null)));

        await profile.LoadAsync(Token);

        Assert.Equal("S", profile.Initial);
        Assert.False(profile.HasPicture);
        Assert.False(profile.RemovePictureCommand.CanExecute(null));
    }

    [Fact]
    public async Task ReadsThePictureAgainWhenItChangesInPlace()
    {
        var profile = new ProfileViewModel(
            Answer<ShowProfile>(new Profile("steve", "/home/steve/.face")),
            Answer<SetPicture>(new Profile("steve", "/home/steve/.face")),
            Answer<RemovePicture>(new Profile("steve", null)));
        await profile.LoadAsync(Token);
        List<string?> changed = [];
        profile.PropertyChanged += (_, change) => changed.Add(change.PropertyName);

        await profile.SetPictureAsync("/tmp/me.png", Token);

        Assert.Contains(nameof(ProfileViewModel.Picture), changed);
        Assert.True(profile.HasPicture);
    }

    [Fact]
    public async Task SaysWhyAPictureIsRefused()
    {
        var profile = new ProfileViewModel(
            Answer<ShowProfile>(new Profile("steve", null)),
            Answer<SetPicture>(new Error("profile.picture_kind", "notes.txt is not a PNG, JPEG or WebP picture.")),
            Answer<RemovePicture>(new Profile("steve", null)));
        await profile.LoadAsync(Token);

        await profile.SetPictureAsync("notes.txt", Token);

        Assert.Equal("notes.txt is not a PNG, JPEG or WebP picture.", profile.Problem);
        Assert.Equal("steve", profile.User);
    }
}
