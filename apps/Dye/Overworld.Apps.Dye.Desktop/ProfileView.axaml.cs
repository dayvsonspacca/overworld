using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Overworld.Apps.Dye.Presentation.Profiles;

namespace Overworld.Apps.Dye.Desktop;

public sealed partial class ProfileView : UserControl
{
    private static readonly FilePickerFileType Pictures = new("Pictures") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"] };

    public ProfileView()
    {
        InitializeComponent();
        Choose.Click += PickPicture;
    }

    private async void PickPicture(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProfileViewModel profile || TopLevel.GetTopLevel(this)?.StorageProvider is not { CanOpen: true } storage)
        {
            return;
        }

        var picked = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Your profile's picture", FileTypeFilter = [Pictures] });

        if (picked is [var file, ..] && file.TryGetLocalPath() is { } path)
        {
            await profile.SetPictureAsync(path, CancellationToken.None);
        }
    }
}
