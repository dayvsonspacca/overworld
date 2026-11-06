using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Overworld.Apps.Crafter.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Desktop.Conversations;

public sealed partial class ConversationView : UserControl
{
    private const double NearTheEnd = 48;

    private ConversationViewModel? _shown;

    public ConversationView()
    {
        InitializeComponent();
        Composer.AddHandler(KeyDownEvent, Typed, RoutingStrategies.Tunnel);
        ModelChip.Click += PickModel;
    }

    public void FocusComposer() => Dispatcher.UIThread.Post(() => Composer.Focus(), DispatcherPriority.Background);

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_shown is not null)
        {
            _shown.Timeline.Entries.CollectionChanged -= Grew;
        }

        _shown = DataContext as ConversationViewModel;

        if (_shown is not null)
        {
            _shown.Timeline.Entries.CollectionChanged += Grew;
            Dispatcher.UIThread.Post(Page.ScrollToEnd, DispatcherPriority.Background);
        }
    }

    private void Typed(object? sender, KeyEventArgs e)
    {
        if (_shown is null)
        {
            return;
        }

        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            if (_shown.SendCommand.CanExecute(null))
            {
                _shown.SendCommand.Execute(null);
                Page.ScrollToEnd();
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None && _shown.IsWorking)
        {
            _shown.StopCommand.Execute(null);
            e.Handled = true;
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

    private void Grew(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Page.Offset.Y >= Page.Extent.Height - Page.Viewport.Height - NearTheEnd)
        {
            Dispatcher.UIThread.Post(Page.ScrollToEnd, DispatcherPriority.Background);
        }
    }
}
