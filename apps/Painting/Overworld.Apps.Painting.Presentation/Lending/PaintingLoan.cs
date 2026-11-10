using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Painting.Presentation.Lending;

public sealed partial class PaintingLoan : ObservableObject, ILoan
{
    private readonly PaintingLender _lender;

    private int? _read;

    public PaintingLoan(Guid id, string title, int version, int versions, string? html, PaintingLender lender)
    {
        _lender = lender;
        Id = id;
        Version = version;
        Title = title;
        Shown = version;
        Versions = Math.Max(versions, version);
        Html = html ?? string.Empty;
    }

    public Guid Id { get; }

    public int Version { get; }

    public string Key => $"painting:{Id}:{Version}";

    public string Source => "Painting";

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    public partial string Html { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewest), nameof(IsPainted))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand), nameof(NextCommand))]
    public partial int Shown { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewest))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    public partial int Versions { get; private set; }

    public bool IsNewest => Shown == Versions;

    public bool IsPainted => Shown == Version;

    [ObservableProperty]
    public partial bool IsLive { get; set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var repaints = _lender.Repaints;
        var shown = await _lender.ShowAsync(Id, Shown, cancellationToken);
        _read = repaints;

        if (shown.IsSuccess)
        {
            Title = shown.Value.Title;
            Html = shown.Value.Html ?? string.Empty;
            Versions = shown.Value.Versions;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private Task PreviousAsync(CancellationToken cancellationToken) => ShowAsync(Shown - 1, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanGoOn))]
    private Task NextAsync(CancellationToken cancellationToken) => ShowAsync(Shown + 1, cancellationToken);

    private bool CanGoBack() => Shown > 1;

    private bool CanGoOn() => Shown < Versions;

    private Task ShowAsync(int version, CancellationToken cancellationToken)
    {
        Shown = version;

        return RefreshAsync(cancellationToken);
    }

    partial void OnIsLiveChanged(bool value)
    {
        if (!value)
        {
            _lender.Repainted -= Repainted;

            return;
        }

        _lender.Repainted += Repainted;

        if (_read != _lender.Repaints || Html.Length == 0)
        {
            _ = RefreshAsync(CancellationToken.None);
        }
    }

    private void Repainted(object? sender, EventArgs e) => _ = RefreshAsync(CancellationToken.None);
}
