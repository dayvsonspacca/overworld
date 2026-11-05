using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Dye.Presentation;

public sealed partial class PalettesViewModel(Operation<ListPalettes, IReadOnlyList<Palette>> list) : ObservableObject
{
    public ObservableCollection<Palette> Palettes { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Swatches))]
    public partial Palette? Selected { get; set; }

    public IReadOnlyList<Swatch> Swatches => Selected is null ? [] : [.. Selected.Colors.Select(Swatch.Of)];

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var listed = await list.ExecuteAsync(new ListPalettes(), cancellationToken);

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

        Selected = Palettes.FirstOrDefault();
    }
}
