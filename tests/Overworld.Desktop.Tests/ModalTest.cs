using Avalonia.Input;
using Avalonia.Interactivity;

namespace Overworld.Desktop.Tests;

public sealed class ModalTest
{
    private static KeyEventArgs Pressed(Key key) => new() { RoutedEvent = InputElement.KeyDownEvent, Key = key };

    [Fact]
    public void AsksToBeDismissedOnEscape()
    {
        TestApp.Run(() =>
        {
            var modal = new Modal();
            var dismissed = 0;
            modal.Dismissed += (_, _) => dismissed++;

            modal.RaiseEvent(Pressed(Key.Escape));

            Assert.Equal(1, dismissed);
        });
    }

    [Fact]
    public void TakesTheEscapeItAnswers()
    {
        TestApp.Run(() =>
        {
            var modal = new Modal();
            var escape = Pressed(Key.Escape);

            modal.RaiseEvent(escape);

            Assert.True(escape.Handled);
        });
    }

    [Fact]
    public void IgnoresOtherKeys()
    {
        TestApp.Run(() =>
        {
            var modal = new Modal();
            var dismissed = 0;
            modal.Dismissed += (_, _) => dismissed++;

            modal.RaiseEvent(Pressed(Key.Enter));

            Assert.Equal(0, dismissed);
        });
    }

    [Fact]
    public void IgnoresClicksOnButtonsOfItsContent()
    {
        TestApp.Run(() =>
        {
            var button = new Avalonia.Controls.Button { Name = "PART_Close" };
            var modal = new Modal { Content = button };
            var dismissed = 0;
            modal.Dismissed += (_, _) => dismissed++;

            button.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));

            Assert.Equal(0, dismissed);
        });
    }

    [Fact]
    public void MarksItselfCompact()
    {
        TestApp.Run(() =>
        {
            var modal = new Modal { IsCompact = true };

            Assert.Contains(":compact", modal.Classes);
        });
    }

    [Fact]
    public void StaysWhenItCannotBeDismissed()
    {
        TestApp.Run(() =>
        {
            var modal = new Modal { IsDismissible = false };
            var dismissed = 0;
            modal.Dismissed += (_, _) => dismissed++;
            var escape = Pressed(Key.Escape);

            modal.RaiseEvent(escape);

            Assert.Equal(0, dismissed);
            Assert.False(escape.Handled);
            Assert.Contains(":fixed", modal.Classes);
        });
    }
}
