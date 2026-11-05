using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Palettes;

[Operation("list", "Lists every saved palette with its colors, in the order they were saved.")]
public sealed class ListPalettesOp(PaletteStore store) : IOperation<ListPalettes, IReadOnlyList<Palette>>
{
    public async Task<Result<IReadOnlyList<Palette>>> ExecuteAsync(ListPalettes input, CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<Palette>>(await store.ListAsync(cancellationToken));
}
