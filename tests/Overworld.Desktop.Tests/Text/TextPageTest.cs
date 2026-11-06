using Avalonia.Input;

namespace Overworld.Desktop.Tests.Text;

public sealed class TextPageTest
{
    [Fact]
    public void GoesToTheEndAndBackToTheStartWithTheKeyboard()
    {
        TestApp.Run(() =>
        {
            var page = Page.With([.. Enumerable.Range(1, 80).Select(line => Page.Paragraph($"Line {line}"))]);
            page.Scroller.Focus();

            page.Press(PhysicalKey.End);
            Assert.True(page.Scroller.Offset.Y > 0);
            Assert.Equal(page.Scroller.Extent.Height - page.Scroller.Viewport.Height, page.Scroller.Offset.Y, precision: 1);

            page.Press(PhysicalKey.Home);
            Assert.Equal(0, page.Scroller.Offset.Y);
        });
    }
}
