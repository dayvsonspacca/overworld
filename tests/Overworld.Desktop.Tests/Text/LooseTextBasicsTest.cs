using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Overworld.Desktop.Tests.Text;

public sealed class LooseTextBasicsTest
{
    private static (Window Window, SelectableTextBlock Text) Shown(string text)
    {
        var block = new SelectableTextBlock { Text = text, Margin = new Thickness(20) };
        var window = new Window { Width = 800, Height = 200, Content = new StackPanel { Children = { block } } };
        window.Show();
        Page.Settle();

        return (window, block);
    }

    private static Point At(Window window, SelectableTextBlock block, string piece)
    {
        var caret = block.TextLayout.HitTestTextPosition(block.Text!.IndexOf(piece, StringComparison.Ordinal) + 1);

        return block.TranslatePoint(new Point(caret.X, caret.Y + (caret.Height / 2)), window)!.Value;
    }

    [Fact]
    public void SelectsAWordWithADoubleClickAndCopiesIt()
    {
        TestApp.Run(() =>
        {
            var (window, block) = Shown("Alpha beta gamma");
            var beta = At(window, block, "beta");

            for (var click = 0; click < 2; click++)
            {
                window.MouseDown(beta, MouseButton.Left);
                window.MouseUp(beta, MouseButton.Left);
            }

            Page.Settle();
            window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
            Page.Settle();

            Assert.Equal("beta", block.SelectedText.Trim());
            Assert.Equal("beta", window.Clipboard!.TryGetTextAsync().GetAwaiter().GetResult()?.Trim());
        });
    }

    [Fact]
    public void SelectsEverythingWithCtrlAOnceFocused()
    {
        TestApp.Run(() =>
        {
            var (window, block) = Shown("Alpha beta gamma");
            var beta = At(window, block, "beta");
            window.MouseDown(beta, MouseButton.Left);
            window.MouseUp(beta, MouseButton.Left);
            Page.Settle();

            window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
            Page.Settle();

            Assert.Equal("Alpha beta gamma", block.SelectedText);
        });
    }

    [Fact]
    public void OffersCopyOnTheContextMenu()
    {
        TestApp.Run(() =>
        {
            var (_, block) = Shown("Alpha");

            Assert.NotNull(block.ContextFlyout ?? (object?)block.ContextMenu);
        });
    }

    [Fact]
    public void PressesTheDefaultButtonOfAModalWithEnter()
    {
        TestApp.Run(() =>
        {
            var pressed = 0;
            var add = new Button { Content = "Add", IsDefault = true };
            add.Click += (_, _) => pressed++;
            var box = new TextBox();
            var window = new Window { Width = 800, Height = 400, Content = new Modal { Title = "Add Project", IsCompact = true, Content = new StackPanel { Children = { box, add } } } };
            window.Show();
            box.Focus();
            Page.Settle();

            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Page.Settle();

            Assert.Equal(1, pressed);
        });
    }
}
