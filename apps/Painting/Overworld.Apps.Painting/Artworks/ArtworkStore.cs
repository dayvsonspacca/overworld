using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Painting.Artworks;

public sealed class ArtworkStore(string directory)
{
    private readonly JsonFile<IReadOnlyList<Artwork>> _file = new(Path.Combine(directory, "paintings.json"), () => []);

    public Task<Result<IReadOnlyList<Artwork>>> ListAsync(CancellationToken cancellationToken) =>
        _file.ReadAsync(cancellationToken);

    public async Task<Result<Artwork>> AddAsync(Artwork artwork, CancellationToken cancellationToken)
    {
        var artworks = await _file.UpdateAsync(
            artworks => Result.Success<IReadOnlyList<Artwork>>([.. artworks, artwork]),
            cancellationToken);

        return artworks.IsSuccess ? artwork : artworks.Error;
    }

    public async Task<Result<Artwork>> ChangeAsync(Guid id, Func<Artwork, Artwork> change, CancellationToken cancellationToken)
    {
        Artwork? changed = null;

        var artworks = await _file.UpdateAsync(
            artworks =>
            {
                if (artworks.FirstOrDefault(artwork => artwork.Id == id) is not { } found)
                {
                    return NotFound(id);
                }

                changed = change(found);

                return Result.Success<IReadOnlyList<Artwork>>([.. artworks.Select(artwork => artwork.Id == id ? changed : artwork)]);
            },
            cancellationToken);

        return artworks.IsSuccess ? changed! : artworks.Error;
    }

    public static Error NotFound(Guid id) => new("painting.not_found", $"There is no painting with the id '{id}'.");
}
