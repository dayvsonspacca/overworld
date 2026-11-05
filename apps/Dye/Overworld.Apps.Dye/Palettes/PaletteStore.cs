using System.Text.Json;
using Overworld.Bedrock.Json;

namespace Overworld.Apps.Dye.Palettes;

public sealed class PaletteStore(string directory)
{
    private readonly string _path = Path.Combine(directory, "palettes.json");

    public async Task<IReadOnlyList<Palette>> ListAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        await using var stream = File.OpenRead(_path);

        return await JsonSerializer.DeserializeAsync<List<Palette>>(stream, BedrockJson.Options, cancellationToken) ?? [];
    }

    public async Task AddAsync(Palette palette, CancellationToken cancellationToken)
    {
        List<Palette> palettes = [.. await ListAsync(cancellationToken), palette];

        Directory.CreateDirectory(directory);

        var temporary = _path + ".tmp";

        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, palettes, BedrockJson.Options, cancellationToken);
        }

        File.Move(temporary, _path, overwrite: true);
    }
}
