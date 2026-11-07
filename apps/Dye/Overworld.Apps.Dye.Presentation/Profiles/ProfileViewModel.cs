using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Dye.Profiles;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Presentation.Profiles;

public sealed partial class ProfileViewModel(
    Operation<ShowProfile, Profile> show,
    Operation<SetPicture, Profile> set,
    Operation<RemovePicture, Profile> remove) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(User), nameof(Initial), nameof(Picture), nameof(HasPicture), nameof(Hint))]
    [NotifyCanExecuteChangedFor(nameof(RemovePictureCommand))]
    public partial Profile? Profile { get; set; }

    public string User => Profile?.User ?? "";

    public string Initial => User.Length > 0 ? User[..1].ToUpperInvariant() : "";

    public string? Picture => Profile?.Picture;

    public bool HasPicture => Picture is not null;

    public string Hint => HasPicture
        ? "Shown in the start menu and on the lock screen. A square picture fits best; it is cut to a circle."
        : "No picture yet; the shell shows your initial. A square picture fits best; it is cut to a circle.";

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken) =>
        Wear(await show.ExecuteAsync(new ShowProfile(), cancellationToken));

    public async Task SetPictureAsync(string file, CancellationToken cancellationToken) =>
        Wear(await set.ExecuteAsync(new SetPicture(file), cancellationToken));

    [RelayCommand(CanExecute = nameof(HasPicture))]
    private async Task RemovePictureAsync(CancellationToken cancellationToken) =>
        Wear(await remove.ExecuteAsync(new RemovePicture(), cancellationToken));

    private void Wear(Result<Profile> profile)
    {
        if (!profile.IsSuccess)
        {
            Problem = profile.Error.Message;

            return;
        }

        Problem = null;

        // The picture keeps its path when it changes, so the profile is cleared first for the window to read it again.
        Profile = null;
        Profile = profile.Value;
    }
}
