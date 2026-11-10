using System.Collections.Concurrent;
using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Apps.Painting.Artworks;

public sealed class ArtworkStore(string directory)
{
    private readonly string _folder = Path.Combine(directory, "paintings");

    private readonly string _legacy = Path.Combine(directory, "paintings.json");

    private readonly ConcurrentDictionary<Guid, JsonFile<Artwork?>> _files = new();

    public async Task<Result<IReadOnlyList<Artwork>>> ListAsync(CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);

        if (!Directory.Exists(_folder))
        {
            return Result.Success<IReadOnlyList<Artwork>>([]);
        }

        List<Artwork> artworks = [];

        foreach (var path in Directory.EnumerateFiles(_folder, "*.json"))
        {
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(path), out var id))
            {
                continue;
            }

            var read = await FileOf(id).ReadAsync(cancellationToken);

            if (!read.IsSuccess)
            {
                return read.Error;
            }

            if (read.Value is { } artwork)
            {
                artworks.Add(artwork);
            }
        }

        return artworks.OrderByDescending(artwork => artwork.Latest.Painted).ToList();
    }

    public async Task<Result<Artwork>> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);
        var read = await FileOf(id).ReadAsync(cancellationToken);

        if (!read.IsSuccess)
        {
            return read.Error;
        }

        return read.Value is { } artwork ? artwork : NotFound(id);
    }

    public async Task<Result<Artwork>> AddAsync(Artwork artwork, CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);
        await FileOf(artwork.Id).WriteAsync(artwork, cancellationToken);

        return artwork;
    }

    public async Task<Result<Artwork>> ChangeAsync(Guid id, Func<Artwork, Artwork> change, CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);

        var changed = await FileOf(id).UpdateAsync(
            artwork => artwork is null ? NotFound(id) : Result.Success<Artwork?>(change(artwork)),
            cancellationToken);

        return changed.IsSuccess ? changed.Value! : changed.Error;
    }

    public static Error NotFound(Guid id) => new("painting.not_found", $"There is no painting with the id '{id}'.");

    private JsonFile<Artwork?> FileOf(Guid id) =>
        _files.GetOrAdd(id, key => new JsonFile<Artwork?>(Path.Combine(_folder, $"{key}.json"), () => null));

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_legacy))
        {
            return;
        }

        var old = await new JsonFile<IReadOnlyList<Legacy>>(_legacy, () => []).ReadAsync(cancellationToken);

        if (!old.IsSuccess)
        {
            return;
        }

        foreach (var painting in old.Value)
        {
            if (!File.Exists(Path.Combine(_folder, $"{painting.Id}.json")))
            {
                await FileOf(painting.Id).WriteAsync(
                    new Artwork(painting.Id, painting.Title, null, [new ArtworkVersion(1, painting.Html, painting.Painted)]),
                    cancellationToken);
            }
        }

        try
        {
            File.Move(_legacy, $"{_legacy}.migrated", overwrite: true);
        }
        catch (Exception moved) when (moved is IOException or UnauthorizedAccessException)
        {
        }
    }

    private sealed record Legacy(Guid Id, string Title, string Html, DateTimeOffset Painted);
}
