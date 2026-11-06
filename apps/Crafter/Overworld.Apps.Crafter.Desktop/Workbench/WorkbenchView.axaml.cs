using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Overworld.Apps.Crafter.Presentation.Workbench;

namespace Overworld.Apps.Crafter.Desktop.Workbench;

public sealed partial class WorkbenchView : UserControl
{
    public WorkbenchView()
    {
        InitializeComponent();
        Adding.Dismissed += (_, _) => (DataContext as WorkbenchViewModel)?.Adding.CloseCommand.Execute(null);
        OtherFolder.Click += PickFolder;
        AddingSearch.AddHandler(KeyDownEvent, Steer, RoutingStrategies.Tunnel);
        Adding.PropertyChanged += (_, change) =>
        {
            if (change.Property == IsVisibleProperty && Adding.IsVisible)
            {
                Dispatcher.UIThread.Post(() => AddingSearch.Focus(), DispatcherPriority.Background);
            }
        };
    }

    public void FocusComposer() => Conversation.FocusComposer();

    public void FocusSearch()
    {
        Search.Focus();
        Search.SelectAll();
    }

    private void Steer(object? sender, KeyEventArgs e)
    {
        if (DataContext is not WorkbenchViewModel { Adding: var adding } || adding.Repositories.Count == 0)
        {
            return;
        }

        var at = adding.Selected is { } selected ? adding.Repositories.IndexOf(selected) : -1;

        if (e.Key is Key.Down or Key.Up)
        {
            var step = e.Key == Key.Down ? 1 : -1;
            adding.Selected = adding.Repositories[(at + step + adding.Repositories.Count) % adding.Repositories.Count];
            AddingList.ScrollIntoView(adding.Selected);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && adding.AddCommand.CanExecute(null))
        {
            adding.AddCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void PickFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkbenchViewModel workbench || TopLevel.GetTopLevel(this)?.StorageProvider is not { CanPickFolder: true } storage)
        {
            return;
        }

        var picked = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "A folder to open as a project" });

        if (picked is [var folder, ..] && folder.TryGetLocalPath() is { } path)
        {
            await workbench.Adding.AddAsync(path);
        }
    }
}
