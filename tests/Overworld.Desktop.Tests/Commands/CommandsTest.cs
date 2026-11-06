using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Overworld.Bedrock.Apps;
using Overworld.Desktop.Commands;
using Page = Overworld.Desktop.Tests.Text.Page;

namespace Overworld.Desktop.Tests.Commands;

public sealed class CommandsTest
{
    private static (Window Window, CommandPalette Palette, TextBox Box) Shown(Action<CommandBook> register)
    {
        var box = new TextBox();
        var window = DesktopHost.Window(new AppIdentity("test"), book =>
        {
            register(book);

            return box;
        });
        window.Show();
        box.Focus();
        Page.Settle();

        return (window, window.GetVisualDescendants().OfType<CommandPalette>().Single(), box);
    }

    private static void Press(Window window, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPressQwerty(key, modifiers);
        Page.Settle();
    }

    [Theory]
    [InlineData("Ctrl+T", 1)]
    [InlineData("Ctrl+K Ctrl+O", 2)]
    [InlineData("Ctrl+/", 1)]
    [InlineData("Escape", 1)]
    [InlineData("Ctrl+Shift+P", 1)]
    public void ReadsAShortcutInOneOrTwoSteps(string keys, int steps)
    {
        var shortcut = Shortcut.Of(keys);

        Assert.Equal(keys, shortcut.Keys);
        Assert.Equal(steps, shortcut.Steps.Count);
    }

    [Fact]
    public void RefusesAShortcutOfThreeSteps()
    {
        Assert.Throws<ArgumentException>(() => Shortcut.Of("Ctrl+K Ctrl+K Ctrl+K"));
    }

    [Theory]
    [InlineData("New chat", "nc", new[] { 0, 4 })]
    [InlineData("New chat", "CHAT", new[] { 4, 5, 6, 7 })]
    [InlineData("New chat", "xyz", null)]
    public void MatchesTheLettersOfAQueryInOrder(string title, string query, int[]? hits)
    {
        var item = PaletteItem.Of(new Command(title, "Chats"), query);

        Assert.Equal(hits, item?.Hits.ToArray());
    }

    [Fact]
    public void RunsAnAppsShortcut()
    {
        TestApp.Run(() =>
        {
            var ran = 0;
            var (window, _, _) = Shown(book => book.Add("New chat", "Chats", () => ran++, "Ctrl+T"));

            Press(window, PhysicalKey.T, RawInputModifiers.Control);

            Assert.Equal(1, ran);
        });
    }

    [Fact]
    public void RunsATwoStepShortcutOnlyWhenBothStepsCome()
    {
        TestApp.Run(() =>
        {
            var ran = 0;
            var (window, _, _) = Shown(book => book.Add("Add a project", "Projects", () => ran++, "Ctrl+K Ctrl+O"));

            Press(window, PhysicalKey.K, RawInputModifiers.Control);
            Assert.Equal(0, ran);
            Press(window, PhysicalKey.O, RawInputModifiers.Control);
            Assert.Equal(1, ran);

            Press(window, PhysicalKey.K, RawInputModifiers.Control);
            Press(window, PhysicalKey.P, RawInputModifiers.Control);
            Assert.Equal(1, ran);
        });
    }

    [Fact]
    public void OpensWithEveryCommandAndRunsTheOneTyped()
    {
        TestApp.Run(() =>
        {
            var ran = new List<string>();
            var (window, palette, _) = Shown(book =>
            {
                book.Add("New chat", "Chats", () => ran.Add("new"), "Ctrl+T");
                book.Add("Close the chat", "Chats", () => ran.Add("close"), "Ctrl+W");
                book.Add("Rename the project", "Projects", () => ran.Add("rename"));
            });

            Press(window, PhysicalKey.P, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.True(palette.IsOpen);
            Assert.Contains(palette.Items, item => item.Command.Title == "Rename the project");

            window.KeyTextInput("close");
            Page.Settle();
            Assert.Equal("Close the chat", palette.Items[0].Command.Title);

            Press(window, PhysicalKey.Enter);
            Assert.Equal(["close"], ran);
            Assert.False(palette.IsOpen);
        });
    }

    [Fact]
    public void OpensWithOnlyTheShortcutsAndTheOnesOfEveryApp()
    {
        TestApp.Run(() =>
        {
            var (window, palette, _) = Shown(book =>
            {
                book.Add("New chat", "Chats", () => { }, "Ctrl+T");
                book.Add("Rename the project", "Projects", () => { });
            });

            Press(window, PhysicalKey.Slash, RawInputModifiers.Control);

            Assert.True(palette.IsOpen);
            Assert.DoesNotContain(palette.Items, item => item.Command.Title == "Rename the project");
            Assert.Contains(palette.Items, item => item.Command.Title == "New chat");
            Assert.Contains(palette.Items, item => item.Command.Group == CommandBook.Everywhere && item.Command.Title == "Copy the selection");
        });
    }

    [Fact]
    public void MovesThroughTheListAndClosesGivingTheFocusBack()
    {
        TestApp.Run(() =>
        {
            var ran = new List<string>();
            var (window, palette, box) = Shown(book =>
            {
                book.Add("Alpha", "Test", () => ran.Add("alpha"));
                book.Add("Beta", "Test", () => ran.Add("beta"));
            });

            Press(window, PhysicalKey.P, RawInputModifiers.Control | RawInputModifiers.Shift);
            Press(window, PhysicalKey.ArrowDown);
            Assert.Equal(1, palette.SelectedIndex);
            Press(window, PhysicalKey.ArrowUp);
            Press(window, PhysicalKey.ArrowUp);
            Assert.Equal(palette.Items.Count - 1, palette.SelectedIndex);

            Press(window, PhysicalKey.Escape);
            Assert.False(palette.IsOpen);
            Assert.True(box.IsFocused);
            Assert.Empty(ran);
        });
    }

    [Fact]
    public void LeavesTheShortcutsOfEveryAppToTheControlsThatAnswerThem()
    {
        TestApp.Run(() =>
        {
            var (window, palette, _) = Shown(_ => { });

            Press(window, PhysicalKey.P, RawInputModifiers.Control | RawInputModifiers.Shift);
            window.KeyTextInput("copy");
            Page.Settle();
            Press(window, PhysicalKey.Enter);

            Assert.True(palette.IsOpen);
        });
    }
}
