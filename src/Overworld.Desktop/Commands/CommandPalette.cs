using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Overworld.Desktop.Commands;

[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_Scrim", typeof(Control))]
[PseudoClasses(":open")]
public sealed class CommandPalette : TemplatedControl
{
    public static readonly StyledProperty<CommandBook?> BookProperty =
        AvaloniaProperty.Register<CommandPalette, CommandBook?>(nameof(Book));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<CommandPalette, bool>(nameof(IsOpen));

    public static readonly StyledProperty<bool> ShortcutsOnlyProperty =
        AvaloniaProperty.Register<CommandPalette, bool>(nameof(ShortcutsOnly));

    public static readonly StyledProperty<string> QueryProperty =
        AvaloniaProperty.Register<CommandPalette, string>(nameof(Query), string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyList<PaletteItem>> ItemsProperty =
        AvaloniaProperty.Register<CommandPalette, IReadOnlyList<PaletteItem>>(nameof(Items), []);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<CommandPalette, int>(nameof(SelectedIndex), -1, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private TextBox? _input;

    private IInputElement? _focusedBefore;

    public CommandPalette()
    {
        AddHandler(KeyDownEvent, Pressed, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, Touched, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(TappedEvent, Picked);
    }

    public CommandBook? Book
    {
        get => GetValue(BookProperty);
        set => SetValue(BookProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool ShortcutsOnly
    {
        get => GetValue(ShortcutsOnlyProperty);
        set => SetValue(ShortcutsOnlyProperty, value);
    }

    public string Query
    {
        get => GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    public IReadOnlyList<PaletteItem> Items
    {
        get => GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public void Open(bool shortcutsOnly)
    {
        _focusedBefore = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        ShortcutsOnly = shortcutsOnly;
        Query = string.Empty;
        IsOpen = true;
        Refresh();
        Dispatcher.UIThread.Post(() => _input?.Focus(), DispatcherPriority.Input);
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _focusedBefore?.Focus();
    }

    public void RunSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Items.Count || Items[SelectedIndex].Command.Run is not { } run)
        {
            return;
        }

        Close();
        run();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _input = e.NameScope.Find<TextBox>("PART_Input");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            PseudoClasses.Set(":open", IsOpen);
        }
        else if (change.Property == QueryProperty || change.Property == BookProperty || change.Property == ShortcutsOnlyProperty)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        var commands = (Book?.All ?? []).Where(command => !ShortcutsOnly || command.Keys is not null);

        Items = [.. commands
            .Select((command, order) => (Item: PaletteItem.Of(command, Query.Trim()), Order: order))
            .Where(each => each.Item is not null)
            .OrderBy(each => each.Item!.Score)
            .ThenBy(each => each.Order)
            .Select(each => each.Item!)];
        SelectedIndex = Items.Count > 0 ? 0 : -1;
    }

    private void Pressed(object? sender, KeyEventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                Close();
                break;
            case Key.Down when Items.Count > 0:
                SelectedIndex = (SelectedIndex + 1) % Items.Count;
                break;
            case Key.Up when Items.Count > 0:
                SelectedIndex = (SelectedIndex - 1 + Items.Count) % Items.Count;
                break;
            case Key.Enter:
                RunSelected();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void Picked(object? sender, TappedEventArgs e)
    {
        if (IsOpen && e.Source is Visual tapped && tapped.FindAncestorOfType<ListBoxItem>(includeSelf: true) is { DataContext: PaletteItem item })
        {
            SelectedIndex = Items.ToList().IndexOf(item);
            RunSelected();
            e.Handled = true;
        }
    }

    private void Touched(object? sender, PointerPressedEventArgs e)
    {
        if (IsOpen && e.Source is Control { Name: "PART_Scrim" } scrim && scrim.TemplatedParent == this)
        {
            Close();
            e.Handled = true;
        }
    }
}
