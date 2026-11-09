using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using Path = Avalonia.Controls.Shapes.Path;

namespace Overworld.Desktop.Text;

public sealed class CopyButton : Button
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<CopyButton, string?>(nameof(Text));

    private static readonly Geometry Sheets = Geometry.Parse("M5.5,5.5 L13.5,5.5 L13.5,13.5 L5.5,13.5 Z M10.5,5.5 L10.5,2.5 L2.5,2.5 L2.5,10.5 L5.5,10.5");

    private static readonly Geometry Check = Geometry.Parse("M3,8.5 L6.5,12 L13,4.5");

    private readonly Path _glyph = new() { Data = Sheets };

    private readonly DispatcherTimer _settling = new() { Interval = TimeSpan.FromSeconds(1.5) };

    public CopyButton()
    {
        Classes.Add("icon");
        Content = _glyph;
        ToolTip.SetTip(this, "Copy");
        _settling.Tick += (_, _) => Show(copied: false);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(Button);

    protected override async void OnClick()
    {
        base.OnClick();

        if (string.IsNullOrEmpty(Text) || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        await clipboard.SetTextAsync(Text);
        Show(copied: true);
    }

    private void Show(bool copied)
    {
        _settling.Stop();
        _glyph.Data = copied ? Check : Sheets;
        Classes.Set("copied", copied);
        ToolTip.SetTip(this, copied ? "Copied" : "Copy");

        if (copied)
        {
            _settling.Start();
        }
    }
}
