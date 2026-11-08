using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Painting.Presentation.Lending;

public sealed partial class PaintingLoan : ObservableObject, ILoan
{
    private readonly PaintingLender _lender;

    private int? _read;

    public PaintingLoan(Artwork artwork, PaintingLender lender)
    {
        _lender = lender;
        Id = artwork.Id;
        Title = artwork.Title;
        Html = artwork.Html;
    }

    public Guid Id { get; }

    public string Key => $"painting:{Id}";

    public string Source => "Painting";

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    public partial string Html { get; private set; }

    [ObservableProperty]
    public partial bool IsLive { get; set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var repaints = _lender.Repaints;
        var shown = await _lender.ShowAsync(Id, cancellationToken);
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
            _lender.Repainted -= Repainted;

            return;
        }

        _lender.Repainted += Repainted;

        if (_read != _lender.Repaints)
        {
            _ = RefreshAsync(CancellationToken.None);
        }
    }

    private void Repainted(object? sender, EventArgs e) => _ = RefreshAsync(CancellationToken.None);
}
