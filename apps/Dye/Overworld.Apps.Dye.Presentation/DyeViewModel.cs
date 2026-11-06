using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Dye.Presentation.Themes;

namespace Overworld.Apps.Dye.Presentation;

public sealed partial class DyeViewModel(PalettesViewModel palettes, ThemesViewModel themes) : ObservableObject
{
    public PalettesViewModel Palettes { get; } = palettes;

    public ThemesViewModel Themes { get; } = themes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsPalettes))]
    public partial bool ShowsThemes { get; set; }

    public bool ShowsPalettes => !ShowsThemes;

    public Task LoadAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(Palettes.LoadAsync(cancellationToken), Themes.LoadAsync(cancellationToken));

    [RelayCommand]
    private void ShowPalettes() => ShowsThemes = false;

    [RelayCommand]
    private async Task ShowThemesAsync(CancellationToken cancellationToken)
    {
        ShowsThemes = true;
        await Themes.LoadAsync(cancellationToken);
    }
}
