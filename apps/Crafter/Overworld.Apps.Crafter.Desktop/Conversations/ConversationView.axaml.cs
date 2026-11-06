using System.Collections.Specialized;
using Avalonia.Controls;
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
    }

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

    private void Grew(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Page.Offset.Y >= Page.Extent.Height - Page.Viewport.Height - NearTheEnd)
        {
            Dispatcher.UIThread.Post(Page.ScrollToEnd, DispatcherPriority.Background);
        }
    }
}
