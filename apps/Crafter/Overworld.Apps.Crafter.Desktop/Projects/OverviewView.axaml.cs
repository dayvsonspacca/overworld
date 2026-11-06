using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Overworld.Apps.Crafter.Presentation.Projects;

namespace Overworld.Apps.Crafter.Desktop.Projects;

public sealed partial class OverviewView : UserControl
{
    private static readonly FilePickerFileType Images = new("Pictures") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp"] };

    public OverviewView()
    {
        InitializeComponent();
        ChooseIcon.Click += PickIcon;
        Removing.Dismissed += (_, _) => (DataContext as OverviewViewModel)?.KeepProjectCommand.Execute(null);
        NameBox.PropertyChanged += (_, change) =>
        {
            if (change.Property == IsVisibleProperty && NameBox.IsEffectivelyVisible)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    NameBox.Focus();
                    NameBox.SelectAll();
                }, DispatcherPriority.Background);
            }
        };
    }

    private async void PickIcon(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel overview || TopLevel.GetTopLevel(this)?.StorageProvider is not { CanOpen: true } storage)
        {
            return;
        }

        var picked = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "The project's icon", FileTypeFilter = [Images] });

        if (picked is [var file, ..] && file.TryGetLocalPath() is { } path)
        {
            await overview.SetIconAsync(path);
        }
    }
}
