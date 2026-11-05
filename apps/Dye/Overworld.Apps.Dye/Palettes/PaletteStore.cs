using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Dye.Palettes;

public sealed class PaletteStore(string directory)
{
    private readonly JsonFile<IReadOnlyList<Palette>> _file = new(Path.Combine(directory, "palettes.json"), () => []);

    public Task<Result<IReadOnlyList<Palette>>> ListAsync(CancellationToken cancellationToken) =>
        _file.ReadAsync(cancellationToken);

    public async Task<Result<Palette>> AddAsync(Palette palette, CancellationToken cancellationToken)
    {
        var palettes = await _file.UpdateAsync(
            palettes => palettes.Any(saved => saved.Name == palette.Name)
                ? new Error("palette.exists", $"A palette named '{palette.Name}' already exists.")
                : Result.Success<IReadOnlyList<Palette>>([.. palettes, palette]),
            cancellationToken);

        return palettes.IsSuccess ? palette : palettes.Error;
    }
}
