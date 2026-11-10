using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Artworks;

[Operation("paint", "Paints a page of HTML for the person to see, or paints a painting again as its next version, keeping the earlier ones, and signals Painting to show it now.")]
public sealed class PaintArtworkOp(ArtworkStore store, BeaconClient beacon, TimeProvider time) : IOperation<PaintArtwork, Canvas>
{
    public async Task<Result<Canvas>> ExecuteAsync(PaintArtwork input, CancellationToken cancellationToken)
    {
        var html = await HtmlAsync(input, cancellationToken);

        if (!html.IsSuccess)
        {
            return html.Error;
        }

        var title = input.Title?.Trim();

        if (title?.Length == 0)
        {
            return new Error("painting.title_empty", "A painting's title can't be blank.");
        }

        var now = time.GetUtcNow();

        var painted = input.Id is { } id
            ? await store.ChangeAsync(id, artwork => (artwork with { Title = title ?? artwork.Title, Group = input.Group ?? artwork.Group }).Painted(html.Value, now), cancellationToken)
            : title is null
                ? new Error("painting.title_missing", "A new painting needs a title.")
                : await store.AddAsync(Artwork.New(title, input.Group, html.Value, now), cancellationToken);

        if (!painted.IsSuccess)
        {
            return painted.Error;
        }

        await beacon.LightAsync("painting", cancellationToken);

        return Canvas.Latest(painted.Value);
    }

    private static async Task<Result<string>> HtmlAsync(PaintArtwork input, CancellationToken cancellationToken)
    {
        if ((input.Html is null) == (input.File is null))
        {
            return new Error("painting.html_or_file", "Give the page either as html or as a file, one of them.");
        }

        if (input.Html is not null)
        {
            return input.Html;
        }

        var file = input.File!.Trim();

        return File.Exists(file)
            ? await File.ReadAllTextAsync(file, cancellationToken)
            : new Error("painting.file_not_found", $"There is no file at '{file}'.");
    }
}
