using Avalonia;
using Avalonia.Controls;
using Overworld.Apps.Crafter.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Desktop.Conversations;

public sealed partial class ConversationDeck : UserControl
{
    public static readonly StyledProperty<ConversationViewModel?> ConversationProperty =
        AvaloniaProperty.Register<ConversationDeck, ConversationViewModel?>(nameof(Conversation));

    private const int Kept = 8;

    private readonly List<ConversationView> _kept = [];

    public ConversationDeck() => InitializeComponent();

    public ConversationViewModel? Conversation
    {
        get => GetValue(ConversationProperty);
        set => SetValue(ConversationProperty, value);
    }

    public void FocusComposer() => _kept.FirstOrDefault(view => view.IsVisible)?.FocusComposer();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ConversationProperty)
        {
            Show(Conversation);
        }
    }

    private void Show(ConversationViewModel? conversation)
    {
        foreach (var view in _kept)
        {
            view.IsVisible = false;
        }

        if (conversation is null)
        {
            return;
        }

        var shown = _kept.FirstOrDefault(view => view.DataContext == conversation);

        if (shown is null)
        {
            shown = new ConversationView();
            Views.Children.Add(shown);
            shown.DataContext = conversation;
        }
        else
        {
            _kept.Remove(shown);
        }

        _kept.Insert(0, shown);
        shown.IsVisible = true;

        foreach (var stale in _kept.Skip(Kept).ToList())
        {
            _kept.Remove(stale);
            Views.Children.Remove(stale);
            stale.DataContext = null;
        }
    }
}
