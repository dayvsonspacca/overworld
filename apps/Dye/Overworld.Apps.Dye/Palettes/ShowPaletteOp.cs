using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Palettes;

[Operation("show", "Shows one palette with its colors, found by its exact name.")]
public sealed class ShowPaletteOp(PaletteStore store) : IOperation<ShowPalette, Palette>
{
    public async Task<Result<Palette>> ExecuteAsync(ShowPalette input, CancellationToken cancellationToken)
    {
        var name = input.Name.Trim();

        var palettes = await store.ListAsync(cancellationToken);

        if (!palettes.IsSuccess)
        {
            return palettes.Error;
        }

        return palettes.Value.FirstOrDefault(palette => palette.Name == name) is { } found
            ? found
            : new Error("palette.not_found", $"There is no palette named '{name}'.");
    }
}
