using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Presentation.Chats;
using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Painting.Presentation;

public sealed partial class PaintingViewModel(Operation<ListArtworks, IReadOnlyList<Canvas>> list, Operation<ShowArtwork, Canvas> show, ChatViewModel? chat = null) : ObservableObject
{
    public const string Ungrouped = "none";

    private IReadOnlyList<Canvas> _all = [];

    public ChatViewModel? Chat { get; } = chat;

    public bool HasChat => Chat is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Designs), nameof(GroupName), nameof(IsInGroup), nameof(IsHome))]
    public partial string? Group { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen), nameof(IsHome), nameof(IsInGroup), nameof(Title), nameof(Html), nameof(IsNewest))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand), nameof(NextCommand), nameof(NewestCommand))]
    public partial Canvas? Open { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<Shelf> Shelves { get; private set; } = [];

    [ObservableProperty]
    public partial Guid? Fresh { get; private set; }

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public bool IsHome => Group is null && Open is null;

    public bool IsInGroup => Group is not null && Open is null;

    public bool IsOpen => Open is not null;

    public bool IsEmpty => _all.Count == 0 && Problem is null;

    public string? GroupName => Group is null ? null : NameOf(Group);

    public string? Title => Open?.Title;

    public string? Html => Open?.Html;

    public bool IsNewest => Open is { } open && open.Version == open.Versions;

    public IReadOnlyList<Canvas> Designs => Group is null ? [] : [.. _all.Where(canvas => KeyOf(canvas.Group) == Group)];

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var listed = await list.ExecuteAsync(new ListArtworks(), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        var before = _all;
        Problem = null;
        _all = listed.Value;
        Shelves = [.. _all.GroupBy(canvas => KeyOf(canvas.Group)).Select(group => new Shelf(group.Key, NameOf(group.Key), group.Count(), [.. group.Take(4).Select(canvas => canvas.Id)])).OrderBy(shelf => shelf.Key == Ungrouped)];
        OnPropertyChanged(nameof(Designs));
        OnPropertyChanged(nameof(IsEmpty));

        if (before.Count > 0 && _all.Count > 0 && _all[0] is var newest && !before.Contains(newest) && newest.Id != Open?.Id)
        {
            Fresh = newest.Id;
        }

        if (Open is { } open && _all.FirstOrDefault(canvas => canvas.Id == open.Id) is { } latest && latest.Versions != open.Versions)
        {
            await ShowAsync(open.Id, open.Version == open.Versions ? null : open.Version, cancellationToken);
        }
    }

    public async Task<string?> PreviewAsync(Guid id)
    {
        var shown = await show.ExecuteAsync(new ShowArtwork(id), CancellationToken.None);

        return shown.IsSuccess ? shown.Value.Html : null;
    }

    [RelayCommand]
    private void Enter(string key)
    {
        Open = null;
        Group = key;
    }

    [RelayCommand]
    private async Task OpenAsync(Guid id, CancellationToken cancellationToken)
    {
        if (Fresh == id)
        {
            Fresh = null;
        }

        await ShowAsync(id, null, cancellationToken);
    }

    [RelayCommand]
    private void Back()
    {
        if (Open is { } open)
        {
            Group ??= KeyOf(open.Group);
            Open = null;

            return;
        }

        Group = null;
    }

    [RelayCommand]
    private void Home()
    {
        Open = null;
        Group = null;
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private Task PreviousAsync(CancellationToken cancellationToken) => ShowAsync(Open!.Id, Open.Version - 1, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanGoOn))]
    private Task NextAsync(CancellationToken cancellationToken) => ShowAsync(Open!.Id, Open.Version + 1, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanGoOn))]
    private Task NewestAsync(CancellationToken cancellationToken) => ShowAsync(Open!.Id, null, cancellationToken);

    [RelayCommand]
    private Task VersionAsync(int version, CancellationToken cancellationToken) => Open is { } open ? ShowAsync(open.Id, version, cancellationToken) : Task.CompletedTask;

    private bool CanGoBack() => Open is { Version: > 1 };

    private bool CanGoOn() => Open is { } open && open.Version < open.Versions;

    private async Task ShowAsync(Guid id, int? version, CancellationToken cancellationToken)
    {
        var shown = await show.ExecuteAsync(new ShowArtwork(id, version), cancellationToken);

        if (!shown.IsSuccess)
        {
            Problem = shown.Error.Message;

            return;
        }

        Problem = null;
        Group ??= KeyOf(shown.Value.Group);
        Open = shown.Value;
    }

    private static string KeyOf(ArtworkGroup? group) => group?.ToString() ?? Ungrouped;

    private static string NameOf(string key) => key == Ungrouped ? "Without a group" : key;
}
