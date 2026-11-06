using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed partial class ViewerViewModel(Func<Picture, string?> fileOf) : ObservableObject
{
    private IReadOnlyList<PictureView> _pictures = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Position), nameof(HasOthers))]
    public partial PictureView? Current { get; private set; }

    [ObservableProperty]
    public partial bool IsOpen { get; private set; }

    public string Position => Current is null ? string.Empty : $"{IndexOf(Current) + 1} / {_pictures.Count}";

    public bool HasOthers => _pictures.Count > 1;

    public void Open(IReadOnlyList<PictureView> pictures, PictureView from)
    {
        _pictures = pictures.Contains(from) ? pictures : [from];
        Current = from;
        IsOpen = true;
    }

    public string? FileOfCurrent() => Current is null ? null : fileOf(Current.Picture);

    [RelayCommand]
    private void Close() => IsOpen = false;

    [RelayCommand]
    private void Next() => Step(1);

    [RelayCommand]
    private void Previous() => Step(-1);

    private void Step(int by)
    {
        if (Current is not null && _pictures.Count > 0)
        {
            Current = _pictures[((IndexOf(Current) + by) % _pictures.Count + _pictures.Count) % _pictures.Count];
        }
    }

    private int IndexOf(PictureView picture) => _pictures.ToList().IndexOf(picture);
}
