using Overworld.Bedrock.Chest;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Palettes;

public sealed class PaletteStore(string directory)
{
    private readonly JsonFile<IReadOnlyList<Palette>> _file = new(Path.Combine(directory, "palettes.json"), () => []);

    public Task<Result<IReadOnlyList<Palette>>> ListAsync(CancellationToken cancellationToken) =>
        _file.ReadAsync(cancellationToken);

    public async Task<Result<Palette>> AddAsync(Palette palette, CancellationToken cancellationToken)
    {
        var palettes = await ListAsync(cancellationToken);

        if (!palettes.IsSuccess)
        {
            return palettes.Error;
        }

        await _file.WriteAsync([.. palettes.Value, palette], cancellationToken);

        return palette;
    }
}
