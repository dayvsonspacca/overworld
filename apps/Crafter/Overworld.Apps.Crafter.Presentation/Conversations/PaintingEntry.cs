using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Presentation.Paintings;
using Overworld.Apps.Painting.Artworks;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed partial class PaintingEntry : Entry
{
    private readonly Gallery _gallery;

    private int? _read;

    public PaintingEntry(Artwork artwork, Gallery gallery)
    {
        _gallery = gallery;
        Id = artwork.Id;
        Title = artwork.Title;
        Html = artwork.Html;
    }

    public Guid Id { get; }

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    public partial string Html { get; private set; }

    [ObservableProperty]
    public partial bool IsLive { get; set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var repaints = _gallery.Repaints;
        var shown = await _gallery.ShowAsync(Id, cancellationToken);
        _read = repaints;

        if (shown.IsSuccess)
        {
            Title = shown.Value.Title;
            Html = shown.Value.Html;
        }
    }

    partial void OnIsLiveChanged(bool value)
    {
        if (!value)
        {
            _gallery.Repainted -= Repainted;

            return;
        }

        _gallery.Repainted += Repainted;

        // What the chat kept may be older than the painting; it is read once when first seen,
        // and again only if it was repainted while out of sight.
        if (_read != _gallery.Repaints)
        {
            _ = RefreshAsync(CancellationToken.None);
        }
    }

    private void Repainted(object? sender, EventArgs e) => _ = RefreshAsync(CancellationToken.None);
}
