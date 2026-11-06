using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Overworld.Apps.Crafter.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Desktop.Conversations;

public sealed partial class PictureViewer : UserControl
{
    private ViewerViewModel? _shown;

    public PictureViewer()
    {
        InitializeComponent();
        Backdrop.PointerPressed += (_, _) => _shown?.CloseCommand.Execute(null);
        Copy.Click += (_, _) => _ = CopyAsync();
        OpenOutside.Click += (_, _) => _ = OpenOutsideAsync();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_shown is not null)
        {
            _shown.PropertyChanged -= Changed;
        }

        _shown = DataContext as ViewerViewModel;

        if (_shown is not null)
        {
            _shown.PropertyChanged += Changed;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_shown is not { IsOpen: true } shown)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                shown.CloseCommand.Execute(null);
                break;
            case Key.Left:
                shown.PreviousCommand.Execute(null);
                break;
            case Key.Right:
                shown.NextCommand.Execute(null);
                break;
            case Key.C when e.KeyModifiers == KeyModifiers.Control:
                _ = CopyAsync();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewerViewModel.IsOpen) && _shown is { IsOpen: true })
        {
            Dispatcher.UIThread.Post(() => Focus(), DispatcherPriority.Background);
        }
    }

    private async Task CopyAsync()
    {
        if (Stage.Source is Bitmap picture && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetBitmapAsync(picture);
        }
    }

    private async Task OpenOutsideAsync()
    {
        if (_shown?.FileOfCurrent() is { } file && TopLevel.GetTopLevel(this)?.Launcher is { } launcher)
        {
            await launcher.LaunchFileInfoAsync(new FileInfo(file));
        }
    }
}
