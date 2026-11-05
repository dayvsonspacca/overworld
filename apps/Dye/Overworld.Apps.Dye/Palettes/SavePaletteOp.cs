using Overworld.Apps.Dye.Colors;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Palettes;

[Operation("save", "Saves a new palette of colors.")]
public sealed class SavePaletteOp(PaletteStore store) : IOperation<SavePalette, Palette>
{
    public async Task<Result<Palette>> ExecuteAsync(SavePalette input, CancellationToken cancellationToken)
    {
        var name = input.Name.Trim();

        if (name.Length == 0)
        {
            return new Error("palette.name_empty", "A palette needs a name.");
        }

        if (input.Colors.Count == 0)
        {
            return new Error("palette.colors_empty", "A palette needs at least one color.");
        }

        List<Color> colors = [];

        foreach (var value in input.Colors)
        {
            var color = Color.Parse(value);

            if (!color.IsSuccess)
            {
                return color.Error;
            }

            colors.Add(color.Value);
        }

        var palettes = await store.ListAsync(cancellationToken);

        if (!palettes.IsSuccess)
        {
            return palettes.Error;
        }

        if (palettes.Value.Any(palette => palette.Name == name))
        {
            return new Error("palette.exists", $"A palette named '{name}' already exists.");
        }

        return await store.AddAsync(new Palette(name, colors), cancellationToken);
    }
}
