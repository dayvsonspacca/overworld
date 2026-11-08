using System.Text.Json;
using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Lending;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Painting.Presentation.Lending;

public sealed class PaintingLender(Operation<ShowArtwork, Artwork> show) : ILender
{
    public const string Signal = "painting";

    public event EventHandler? Repainted;

    public int Repaints { get; private set; }

    public ILoan? Lend(JsonElement said) =>
        said.ValueKind == JsonValueKind.Object
            && said.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && id.TryGetGuid(out var painting)
            && said.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
            && said.TryGetProperty("html", out var html) && html.ValueKind == JsonValueKind.String
            && said.TryGetProperty("painted", out var painted) && painted.ValueKind == JsonValueKind.String && painted.TryGetDateTimeOffset(out var when)
                ? new PaintingLoan(new Artwork(painting, title.GetString()!, html.GetString()!, when), this)
                : null;

    public Task<Result<Artwork>> ShowAsync(Guid id, CancellationToken cancellationToken) =>
        show.ExecuteAsync(new ShowArtwork(id), cancellationToken);

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
