using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Overworld.Desktop.Text;

public sealed class Caret
{
    public static readonly AttachedProperty<bool> IsWholeProperty =
        AvaloniaProperty.RegisterAttached<Caret, TextBox, bool>("IsWhole");

    private static readonly ConditionalWeakTable<TextBox, Caret> Drawn = [];

    private static readonly TimeSpan Blink = TimeSpan.FromMilliseconds(500);

    private readonly TextBox _box;

    private readonly Rectangle _bar = new()
    {
        Width = 1,
        IsHitTestVisible = false,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };

    private readonly DispatcherTimer _blink;

    private TextPresenter? _presenter;

    static Caret() => IsWholeProperty.Changed.AddClassHandler<TextBox>((box, change) =>
    {
        if (change.GetNewValue<bool>())
        {
            _ = Drawn.GetValue(box, drawn => new Caret(drawn));
        }
    });

    private Caret(TextBox box)
    {
        _box = box;
        _box.CaretBrush = Brushes.Transparent;
        _bar.Bind(Shape.FillProperty, box.GetObservable(TemplatedControl.ForegroundProperty));
        _blink = new DispatcherTimer(Blink, DispatcherPriority.Normal, (_, _) => _bar.Opacity = _bar.Opacity > 0 ? 0 : 1);
        _box.TemplateApplied += (_, e) => Hold(e.NameScope.Find<TextPresenter>("PART_TextPresenter"));
        _box.PropertyChanged += Changed;
        _box.DetachedFromVisualTree += (_, _) => _blink.Stop();
        Hold(box.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault());
    }

    public static bool GetIsWhole(TextBox box) => box.GetValue(IsWholeProperty);

    public static void SetIsWhole(TextBox box, bool value) => box.SetValue(IsWholeProperty, value);

    private void Hold(TextPresenter? presenter)
    {
        if (presenter == _presenter)
        {
            return;
        }

        if (_presenter is not null)
        {
            _presenter.LayoutUpdated -= Place;
        }

        (_bar.Parent as Panel)?.Children.Remove(_bar);
        _presenter = presenter;

        if (_presenter?.Parent is Panel panel)
        {
            panel.Children.Add(_bar);
            _presenter.LayoutUpdated += Place;
        }

        Restart();
    }

    private void Changed(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == TextBox.CaretIndexProperty
            || change.Property == TextBox.TextProperty
            || change.Property == TextBox.SelectionStartProperty
            || change.Property == TextBox.SelectionEndProperty
            || change.Property == InputElement.IsFocusedProperty)
        {
            Restart();
        }
    }

    private void Restart()
    {
        _bar.Opacity = 1;
        _blink.Stop();
        Place(null, EventArgs.Empty);

        if (_bar.IsVisible)
        {
            _blink.Start();
        }
    }

    private void Place(object? sender, EventArgs e)
    {
        var shown = _presenter is not null && _box.IsFocused && _box.SelectionStart == _box.SelectionEnd;
        _bar.IsVisible = shown;

        if (!shown || _presenter is null || _bar.Parent is not Visual panel || _presenter.TranslatePoint(default, panel) is not { } origin)
        {
            return;
        }

        var bounds = _presenter.TextLayout.HitTestTextPosition(Math.Clamp(_box.CaretIndex, 0, _box.Text?.Length ?? 0));
        var x = Math.Min(Math.Ceiling(origin.X + bounds.X), Math.Max(0, origin.X + _presenter.Bounds.Width - _bar.Width));
        var margin = new Thickness(x, Math.Floor(origin.Y + bounds.Y), 0, 0);
        var height = Math.Ceiling(bounds.Height);

        if (_bar.Margin != margin)
        {
            _bar.Margin = margin;
        }

        if (!_bar.Height.Equals(height))
        {
            _bar.Height = height;
        }
    }
}
