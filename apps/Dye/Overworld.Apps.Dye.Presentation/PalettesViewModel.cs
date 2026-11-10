using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Dye.Presentation;

public sealed partial class PalettesViewModel : ObservableObject
{
    private readonly Operation<ListPalettes, IReadOnlyList<Palette>> _list;

    public PalettesViewModel(
        Operation<ListPalettes, IReadOnlyList<Palette>> list,
        Operation<SavePalette, Palette> save)
    {
        _list = list;
        New = new NewPaletteViewModel(save);
        New.Saved += async (_, saved) => await LoadAsync(saved.Name, CancellationToken.None);
    }

    public ObservableCollection<Palette> Palettes { get; } = [];

    public NewPaletteViewModel New { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Swatches))]
    public partial Palette? Selected { get; set; }

    public IReadOnlyList<Swatch> Swatches => Selected is null ? [] : [.. Selected.Colors.Select(Swatch.Of)];

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public Task LoadAsync(CancellationToken cancellationToken) => LoadAsync(select: null, cancellationToken);

    private async Task LoadAsync(string? select, CancellationToken cancellationToken)
    {
        var listed = await _list.ExecuteAsync(new ListPalettes(), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        Problem = null;
        Palettes.Clear();

        foreach (var palette in listed.Value)
        {
            Palettes.Add(palette);
        }

        var open = select ?? Selected?.Name;
        Selected = Palettes.FirstOrDefault(palette => palette.Name == open);
    }

    [RelayCommand]
    private void Close() => Selected = null;
}
