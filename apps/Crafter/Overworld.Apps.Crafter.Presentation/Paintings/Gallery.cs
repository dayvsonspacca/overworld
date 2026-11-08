using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Presentation.Paintings;

// Painting's paintings as Crafter reaches them: one read by its id, and word that one was repainted.
public sealed class Gallery(Operation<ShowArtwork, Artwork> show)
{
    public const string Signal = "painting";

    public event EventHandler? Repainted;

    // How many times a painting was repainted since Crafter opened, so a frame that slept through
    // some knows to read its painting again when it wakes.
    public int Repaints { get; private set; }

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
