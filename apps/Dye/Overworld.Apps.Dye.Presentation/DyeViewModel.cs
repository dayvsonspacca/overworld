using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Dye.Presentation.Profiles;
using Overworld.Apps.Dye.Presentation.Themes;

namespace Overworld.Apps.Dye.Presentation;

public sealed partial class DyeViewModel(PalettesViewModel palettes, ThemesViewModel themes, ProfileViewModel profile) : ObservableObject
{
    public enum Page
    {
        Palettes,
        Themes,
        Profile,
    }

    public PalettesViewModel Palettes { get; } = palettes;

    public ThemesViewModel Themes { get; } = themes;

    public ProfileViewModel Profile { get; } = profile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsPalettes), nameof(ShowsThemes), nameof(ShowsProfile))]
    public partial Page Shown { get; set; }

    public bool ShowsPalettes => Shown == Page.Palettes;

    public bool ShowsThemes => Shown == Page.Themes;

    public bool ShowsProfile => Shown == Page.Profile;

    public Task LoadAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(Palettes.LoadAsync(cancellationToken), Themes.LoadAsync(cancellationToken), Profile.LoadAsync(cancellationToken));

    [RelayCommand]
    private void ShowPalettes() => Shown = Page.Palettes;

    [RelayCommand]
    private async Task ShowThemesAsync(CancellationToken cancellationToken)
    {
        Shown = Page.Themes;
        await Themes.LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task ShowProfileAsync(CancellationToken cancellationToken)
    {
        Shown = Page.Profile;
        await Profile.LoadAsync(cancellationToken);
    }
}
