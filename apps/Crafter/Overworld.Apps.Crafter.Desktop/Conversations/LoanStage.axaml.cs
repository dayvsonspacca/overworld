using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Overworld.Apps.Crafter.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Desktop.Conversations;

public sealed partial class LoanStage : UserControl
{
    private ConversationViewModel? _shown;

    public LoanStage() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_shown is not null)
        {
            _shown.PropertyChanged -= Changed;
        }

        _shown = DataContext as ConversationViewModel;

        if (_shown is not null)
        {
            _shown.PropertyChanged += Changed;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel.GetTopLevel(this)?.AddHandler(KeyDownEvent, Escaped, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        TopLevel.GetTopLevel(this)?.RemoveHandler(KeyDownEvent, Escaped);
    }

    private void Escaped(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _shown is { Spread: not null } shown)
        {
            shown.CloseSpreadCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConversationViewModel.Spread) && _shown is { Spread: not null })
        {
            Dispatcher.UIThread.Post(() => Focus(), DispatcherPriority.Background);
        }
    }
}
