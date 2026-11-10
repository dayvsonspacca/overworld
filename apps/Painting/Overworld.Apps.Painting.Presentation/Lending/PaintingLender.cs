using System.Text.Json;
using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Lending;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Presentation.Lending;

public sealed class PaintingLender(Operation<ShowArtwork, Canvas> show) : ILender
{
    public const string Signal = "painting";

    public event EventHandler? Repainted;

    public int Repaints { get; private set; }

    public ILoan? Lend(JsonElement said)
    {
        if (said.ValueKind != JsonValueKind.Object
            || !said.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || !id.TryGetGuid(out var painting)
            || !said.TryGetProperty("title", out var title) || title.ValueKind != JsonValueKind.String
            || !said.TryGetProperty("painted", out var painted) || painted.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var version = said.TryGetProperty("version", out var number) && number.TryGetInt32(out var pinned) ? pinned : 1;
        var versions = said.TryGetProperty("versions", out var count) && count.TryGetInt32(out var total) ? total : version;
        var html = said.TryGetProperty("html", out var page) && page.ValueKind == JsonValueKind.String ? page.GetString() : null;

        return new PaintingLoan(painting, title.GetString()!, version, versions, html, this);
    }

    public Task<Result<Canvas>> ShowAsync(Guid id, int version, CancellationToken cancellationToken) =>
        show.ExecuteAsync(new ShowArtwork(id, version), cancellationToken);

    public async Task FollowAsync(IAsyncEnumerable<string> signals, CancellationToken cancellationToken)
    {
        await foreach (var signal in signals.WithCancellation(cancellationToken))
        {
            if (signal == Signal)
            {
                Repaints++;
                Repainted?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
