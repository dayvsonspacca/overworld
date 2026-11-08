using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Dye.Presentation.Lending;

public sealed partial class PaletteLoan(Palette palette) : ObservableObject, ILoan
{
    public string Key => $"dye:palette:{palette.Name}";

    public string Source => "Dye";

    public string Title => palette.Colors.Count == 1 ? $"{palette.Name} · 1 color" : $"{palette.Name} · {palette.Colors.Count} colors";

    public IReadOnlyList<Swatch> Swatches { get; } = [.. palette.Colors.Select(Swatch.Of)];

    [ObservableProperty]
    public partial bool IsLive { get; set; }
}
