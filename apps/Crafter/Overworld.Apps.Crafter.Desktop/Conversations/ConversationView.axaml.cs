using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Composing;
using Overworld.Apps.Crafter.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Desktop.Conversations;

public sealed partial class ConversationView : UserControl
{
    private const double NearTheEnd = 48;

    private const double NearTheTop = 400;

    private ConversationViewModel? _shown;

    private bool _settled;

    private bool _filling;

    public ConversationView()
    {
        InitializeComponent();
        Composer.AddHandler(KeyDownEvent, Typed, RoutingStrategies.Tunnel);
        Composer.AddHandler(TextBox.PastingFromClipboardEvent, Paste);
        Composer.PropertyChanged += Moved;
        Suggestions.AddHandler(PointerPressedEvent, Chose, RoutingStrategies.Tunnel);
        ModelChip.Click += PickModel;
        AddHandler(DragDrop.DragEnterEvent, Hover);
        AddHandler(DragDrop.DragOverEvent, Hover);
        AddHandler(DragDrop.DragLeaveEvent, (_, _) => DropZone.IsVisible = false);
        AddHandler(DragDrop.DropEvent, Drop);
        Page.ScrollChanged += Scrolled;
    }

    public void FocusComposer() => Dispatcher.UIThread.Post(() => Composer.Focus(), DispatcherPriority.Background);

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_shown is not null)
        {
            _shown.Timeline.Entries.CollectionChanged -= Grew;
            _shown.Mentions.Close();
        }

        _shown = DataContext as ConversationViewModel;
        _settled = false;

        if (_shown is not null)
        {
            _shown.Timeline.Entries.CollectionChanged += Grew;
            Dispatcher.UIThread.Post(Settle, DispatcherPriority.Background);
        }
    }

    private void Settle()
    {
        Page.ScrollToEnd();
        _settled = true;
        Scrolled(null, null);
    }

    private void Typed(object? sender, KeyEventArgs e)
    {
        if (_shown is null || e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }

        if (_shown.Mentions.IsOpen)
        {
            switch (e.Key)
            {
                case Key.Down:
                    _shown.Mentions.Move(1);
                    e.Handled = true;
                    return;
                case Key.Up:
                    _shown.Mentions.Move(-1);
                    e.Handled = true;
                    return;
                case Key.Enter or Key.Tab:
                    Accept(null);
                    e.Handled = true;
                    return;
                case Key.Escape:
                    _shown.Mentions.Close();
                    e.Handled = true;
                    return;
            }
        }

        if (e.Key == Key.Enter)
        {
            if (_shown.SendCommand.CanExecute(null))
            {
                _shown.SendCommand.Execute(null);
                Page.ScrollToEnd();
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _shown.IsWorking)
        {
            _shown.StopCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Moved(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (_shown is not null && (change.Property == TextBox.TextProperty || change.Property == TextBox.CaretIndexProperty) && Composer.IsFocused)
        {
            _shown.Mentions.Look(Composer.Text ?? string.Empty, Composer.CaretIndex);
        }
    }

    private void Chose(object? sender, PointerPressedEventArgs e)
    {
        if ((e.Source as Visual)?.FindAncestorOfType<Border>(includeSelf: true) is { Tag: MentionViewModel } row)
        {
            Accept((MentionViewModel)row.Tag!);
            e.Handled = true;
        }
    }

    private void Accept(MentionViewModel? chosen)
    {
        if (_shown?.Mentions.Accept(Composer.Text ?? string.Empty, chosen) is not var (draft, caret))
        {
            return;
        }

        _shown.Draft = draft;
        Composer.Text = draft;
        Composer.CaretIndex = caret;
        Composer.Focus();
        _shown.Mentions.Look(draft, caret);
    }

    private async void Paste(object? sender, RoutedEventArgs e)
    {
        if (_shown is not { } shown || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        if (await clipboard.TryGetFilesAsync() is { Length: > 0 } items)
        {
            e.Handled = true;
            Dropped(shown, Local(items));
        }
        else if (await clipboard.TryGetTextAsync() is { Length: > 0 })
        {
            return;
        }
        else if (await clipboard.TryGetBitmapAsync() is { } bitmap)
        {
            e.Handled = true;

            using (bitmap)
            {
                if (Pictures.Of(bitmap) is { } picture)
                {
                    shown.Attach(picture);
                }
            }
        }
    }

    private void Hover(object? sender, DragEventArgs e)
    {
        var takes = _shown is not null && (e.DataTransfer.Contains(DataFormat.File) || e.DataTransfer.Contains(DataFormat.Text));
        e.DragEffects = takes ? DragDropEffects.Copy : DragDropEffects.None;
        DropZone.IsVisible = takes;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        DropZone.IsVisible = false;

        if (_shown is not { } shown)
        {
            return;
        }

        e.Handled = true;
        Composer.Focus();

        if (e.DataTransfer.TryGetFiles() is { Length: > 0 } items)
        {
            Dropped(shown, Local(items));
        }
        else if (e.DataTransfer.TryGetText() is { Length: > 0 } text && DroppedText.IsReadable(text))
        {
            if (DroppedText.Paths(text) is { Count: > 0 } paths)
            {
                Dropped(shown, paths);
            }
            else
            {
                var at = Math.Clamp(Composer.CaretIndex, 0, shown.Draft.Length);
                shown.Draft = shown.Draft.Insert(at, text);
            }
        }
    }

    private static IEnumerable<string> Local(IEnumerable<IStorageItem> items) => items.Select(item => item.TryGetLocalPath()).OfType<string>();

    private static void Dropped(ConversationViewModel shown, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            shown.Attach(Pictures.IsPicture(path) && Pictures.Of(path) is { } picture ? picture : new FileMention(path));
        }
    }

    private void PickModel(object? sender, RoutedEventArgs e)
    {
        if (_shown is null)
        {
            return;
        }

        var menu = new MenuFlyout();

        foreach (var option in _shown.Models)
        {
            menu.Items.Add(new MenuItem
            {
                Header = option.Label,
                Command = _shown.PickModelCommand,
                CommandParameter = option,
                Icon = _shown.ModelLabel == option.Label ? new TextBlock { Text = "✓" } : null,
            });
        }

        menu.ShowAt(ModelChip);
    }

    private void Scrolled(object? sender, ScrollChangedEventArgs? e)
    {
        if (!_settled || _filling)
        {
            return;
        }

        _filling = true;

        try
        {
            while (_shown is { Timeline.HasEarlier: true } shown && Page.Offset.Y <= NearTheTop)
            {
                var below = Page.Extent.Height - Page.Offset.Y;
                shown.Timeline.ShowEarlier();
                Page.UpdateLayout();
                Page.Offset = Page.Offset.WithY(Page.Extent.Height - below);
            }

            PutAway();
        }
        finally
        {
            _filling = false;
        }
    }

    // Back at the end, what was brought back from earlier is put away again, so a long chat doesn't keep
    // laying out entries no one is reading; enough stays above that it doesn't come straight back.
    private void PutAway()
    {
        if (_shown is not { } shown || shown.Timeline.Shown.Count <= Timeline.Page || !AtTheEnd())
        {
            return;
        }

        var room = Page.Offset.Y - (2 * NearTheTop);
        var fewer = 0;

        while (Entries.ContainerFromIndex(fewer) is { } entry && entry.DesiredSize.Height <= room)
        {
            room -= entry.DesiredSize.Height;
            fewer++;
        }

        if (shown.Timeline.ShowFewer(fewer) > 0)
        {
            Page.ScrollToEnd();
        }
    }

    private bool AtTheEnd() => Page.Offset.Y >= Page.Extent.Height - Page.Viewport.Height - NearTheEnd;

    private void Grew(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (AtTheEnd())
        {
            Dispatcher.UIThread.Post(Page.ScrollToEnd, DispatcherPriority.Background);
        }
    }
}
