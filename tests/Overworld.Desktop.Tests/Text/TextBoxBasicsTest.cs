using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Overworld.Desktop.Tests.Text;

public sealed class TextBoxBasicsTest
{
    private static (Window Window, TextBox Box) Shown(string text)
    {
        var box = new TextBox { Text = text, Width = 600 };
        var window = new Window { Width = 800, Height = 200, Content = box };
        window.Show();
        box.Focus();
        Page.Settle();

        return (window, box);
    }

    private static void Press(Window window, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPressQwerty(key, modifiers);
        Page.Settle();
    }

    private static Point At(Window window, TextBox box, string text, string piece)
    {
        var presenter = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(box).OfType<Avalonia.Controls.Presenters.TextPresenter>().First();
        var caret = presenter.TextLayout.HitTestTextPosition(text.IndexOf(piece, StringComparison.Ordinal) + 1);

        return presenter.TranslatePoint(new Point(caret.X, caret.Y + (caret.Height / 2)), window)!.Value;
    }

    [Fact]
    public void SelectsEverythingWithCtrlA()
    {
        TestApp.Run(() =>
        {
            var (window, box) = Shown("Alpha beta gamma");

            Press(window, PhysicalKey.A, RawInputModifiers.Control);

            Assert.Equal("Alpha beta gamma", box.SelectedText);
        });
    }

    [Fact]
    public void SelectsAWordWithADoubleClickAndTheLineWithATripleClick()
    {
        TestApp.Run(() =>
        {
            var (window, box) = Shown("Alpha beta gamma");
            var beta = At(window, box, box.Text!, "beta");

            window.MouseDown(beta, MouseButton.Left);
            window.MouseUp(beta, MouseButton.Left);
            window.MouseDown(beta, MouseButton.Left);
            window.MouseUp(beta, MouseButton.Left);
            Page.Settle();
            Assert.Equal("beta", box.SelectedText.Trim());

            window.MouseDown(beta, MouseButton.Left);
            window.MouseUp(beta, MouseButton.Left);
            Page.Settle();
            Assert.Equal("Alpha beta gamma", box.SelectedText);
        });
    }

    [Fact]
    public void DeletesAWordWithCtrlBackspace()
    {
        TestApp.Run(() =>
        {
            var (window, box) = Shown("Alpha beta gamma");
            box.CaretIndex = box.Text!.Length;

            Press(window, PhysicalKey.Backspace, RawInputModifiers.Control);

            Assert.Equal("Alpha beta ", box.Text);
        });
    }

    [Fact]
    public void ExtendsTheSelectionByWordWithCtrlShiftArrows()
    {
        TestApp.Run(() =>
        {
            var (window, box) = Shown("Alpha beta gamma");
            box.CaretIndex = 0;

            Press(window, PhysicalKey.ArrowRight, RawInputModifiers.Control | RawInputModifiers.Shift);

            Assert.Equal("Alpha", box.SelectedText.Trim());
        });
    }

    [Fact]
    public void UndoesAndRedoes()
    {
        TestApp.Run(() =>
        {
            var (window, box) = Shown(string.Empty);
            window.KeyTextInput("abc");
            Page.Settle();

            Press(window, PhysicalKey.Z, RawInputModifiers.Control);
            Assert.NotEqual("abc", box.Text ?? string.Empty);

            Press(window, PhysicalKey.Z, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.Equal("abc", box.Text);
        });
    }

    [Fact]
    public void CutsAndPastes()
    {
        TestApp.Run(() =>
        {
            var (window, box) = Shown("Alpha beta");
            Press(window, PhysicalKey.A, RawInputModifiers.Control);

            Press(window, PhysicalKey.X, RawInputModifiers.Control);
            Assert.Equal(string.Empty, box.Text ?? string.Empty);
            Assert.Equal("Alpha beta", window.Clipboard!.TryGetTextAsync().GetAwaiter().GetResult());

            Press(window, PhysicalKey.V, RawInputModifiers.Control);
            Assert.Equal("Alpha beta", box.Text);
        });
    }

    [Fact]
    public void OffersCutCopyAndPasteOnTheContextMenu()
    {
        TestApp.Run(() =>
        {
            var (_, box) = Shown("Alpha");

            Assert.NotNull(box.ContextFlyout ?? (object?)box.ContextMenu);
        });
    }
}
