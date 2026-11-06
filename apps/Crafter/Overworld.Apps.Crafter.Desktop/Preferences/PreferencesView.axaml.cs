using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Overworld.Apps.Crafter.Presentation.Preferences;

namespace Overworld.Apps.Crafter.Desktop.Preferences;

public sealed partial class PreferencesView : UserControl
{
    public PreferencesView()
    {
        InitializeComponent();
        ChooseRoot.Click += PickRoot;
    }

    private async void PickRoot(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PreferencesViewModel preferences || TopLevel.GetTopLevel(this)?.StorageProvider is not { CanPickFolder: true } storage)
        {
            return;
        }

        var picked = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "The projects folder" });

        if (picked is [var folder, ..] && folder.TryGetLocalPath() is { } path)
        {
            await preferences.SetProjectsRootAsync(path);
        }
    }
}
