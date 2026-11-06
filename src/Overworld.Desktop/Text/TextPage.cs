using Avalonia.Controls;
using Avalonia.Input;

namespace Overworld.Desktop.Text;

public sealed class TextPage : ScrollViewer
{
    public TextPage() => Selection = TextSelection.Watch(this);

    public TextSelection Selection { get; }

    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control && e.Key is Key.Home or Key.End)
        {
            if (e.Key == Key.Home)
            {
                ScrollToHome();
            }
            else
            {
                ScrollToEnd();
            }

            e.Handled = true;

            return;
        }

        base.OnKeyDown(e);
    }
}
