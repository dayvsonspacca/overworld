using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Painting.Presentation;

public sealed partial class PaintingViewModel(Operation<ShowArtwork, Artwork> show) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Html), nameof(IsEmpty))]
    public partial Artwork? Artwork { get; set; }

    public string? Title => Artwork?.Title;

    public string? Html => Artwork?.Html;

    public bool IsEmpty => Artwork is null && Problem is null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial string? Problem { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var shown = await show.ExecuteAsync(new ShowArtwork(), cancellationToken);

        if (shown.IsSuccess)
        {
            Problem = null;
            Artwork = shown.Value;

            return;
        }

        Artwork = null;
        Problem = shown.Error.Code == ShowArtworkOp.Empty ? null : shown.Error.Message;
    }
}
