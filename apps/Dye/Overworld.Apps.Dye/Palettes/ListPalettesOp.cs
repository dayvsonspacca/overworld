using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Palettes;

[Operation("list", "Lists every saved palette with its colors, in the order they were saved.")]
public sealed class ListPalettesOp(PaletteStore store) : IOperation<ListPalettes, IReadOnlyList<Palette>>
{
    public Task<Result<IReadOnlyList<Palette>>> ExecuteAsync(ListPalettes input, CancellationToken cancellationToken) =>
        store.ListAsync(cancellationToken);
}
