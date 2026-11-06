using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Overworld.Desktop.Text;

namespace Overworld.Desktop.Tests.Text;

public sealed class TextSelectionTest
{
    [Fact]
    public void CopiesOnlyTheWordsSelectedInAListItem()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.List("Olá, bão?", "Segundo item"));

            page.Drag(page.Before("bão"), page.After("bão"));

            Assert.Equal("bão", page.Copied());
        });
    }

    [Fact]
    public void CopiesTheMarkerWhenTheSelectionStartsOnIt()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.List("Olá, bão?", "Segundo item"));

            page.Drag(page.LeftOf(page.Marker("Olá")), page.After("bão?"));

            Assert.Equal("• Olá, bão?", page.Copied());
        });
    }

    [Fact]
    public void CopiesItemsAsLinesAndOtherBlocksAsParagraphs()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.List("Olá, bão?", "Segundo item"), Page.Paragraph("Depois da lista."));

            page.Drag(page.Before("bão"), page.After("Depois"));

            Assert.Equal("bão?\n• Segundo item\n\nDepois", page.Copied());
        });
    }

    [Fact]
    public void CopiesAcrossBoldAndPlainTextExactly()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.Bold("Duplo clique seleciona a palavra", ", copiando com Ctrl+C."));

            page.Drag(page.Before("clique"), page.After("copiando"));

            Assert.Equal("clique seleciona a palavra, copiando", page.Copied());
        });
    }

    [Fact]
    public void SelectsForwardsAndBackwardsAlike()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.Paragraph("First paragraph here."), Page.Paragraph("Second paragraph here."));

            page.Drag(page.Before("paragraph here.", "First"), page.After("Second"));
            Assert.Equal("paragraph here.\n\nSecond", page.Copied());

            page.Drag(page.After("Second"), page.Before("paragraph here.", "First"));
            Assert.Equal("paragraph here.\n\nSecond", page.Copied());
        });
    }

    [Fact]
    public void DoubleClickSelectsTheWordAndTripleClickTheBlock()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.Paragraph("Escolha a opção, por favor."), Page.Paragraph("Outro parágrafo."));

            page.Click(page.Before("opção"), times: 2);
            Assert.Equal("opção", page.Copied());

            page.Click(page.Before("opção"), times: 3);
            Assert.Equal("Escolha a opção, por favor.", page.Copied());
        });
    }

    [Fact]
    public void ShiftClickExtendsTheSelection()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.Paragraph("Alpha beta gamma delta."));

            page.Click(page.Before("beta"));
            page.Click(page.After("gamma"), modifiers: RawInputModifiers.Shift);

            Assert.Equal("beta gamma", page.Copied());
        });
    }

    [Fact]
    public void SelectAllTakesEveryBlock()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.Paragraph("Title"), Page.Paragraph("Body text."), Page.List("one", "two"));

            page.Click(page.Before("Body"), times: 2);
            page.Press(PhysicalKey.A);

            Assert.Equal("Title\n\nBody text.\n\n• one\n• two", page.Copied());
        });
    }

    [Fact]
    public void AClickOrEscapeClearsTheSelectionAndLeavesTheClipboardAlone()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.Paragraph("Alpha beta gamma."));
            page.Paste("before");

            page.Drag(page.Before("beta"), page.After("gamma"));
            page.Click(page.Before("Alpha"));
            Assert.Equal("before", page.Copied());
            Assert.False(page.Selection.HasSelection);

            page.Drag(page.Before("beta"), page.After("gamma"));
            page.Press(PhysicalKey.Escape, RawInputModifiers.None);
            Assert.False(page.Selection.HasSelection);
        });
    }

    [Fact]
    public void PressesOnButtonsStayTheirs()
    {
        TestApp.Run(() =>
        {
            var clicked = 0;
            var button = new Button { Content = "Retry" };
            button.Click += (_, _) => clicked++;
            var page = Page.With(Page.Paragraph("Some answer."), button);

            page.Click(button.TranslatePoint(new Point(10, 10), page.Window)!.Value);

            Assert.Equal(1, clicked);
            Assert.False(page.Selection.HasSelection);
        });
    }

    [Fact]
    public void DrawsTheSelectionOnEveryBlockItCovers()
    {
        TestApp.Run(() =>
        {
            var page = Page.With(Page.List("Olá, bão?", "Segundo item"));

            page.Drag(page.Before("bão"), page.After("Segundo"));

            Assert.Equal((5, 9), Span(page.Block("Olá")));
            Assert.Equal((0, 1), Span(page.Marker("Segundo")));
            Assert.Equal((0, 7), Span(page.Block("Segundo")));
            Assert.Equal((0, 0), Span(page.Marker("Olá")));
        });
    }

    [Fact]
    public void APlainClickLeavesTheFocusAndASelectionTakesIt()
    {
        TestApp.Run(() =>
        {
            var box = new TextBox { Text = "draft" };
            var page = Page.With(box, Page.Paragraph("Alpha beta gamma."));
            box.Focus();

            page.Click(page.Before("beta"));
            Assert.True(box.IsFocused);

            page.Drag(page.Before("beta"), page.After("gamma"));
            Assert.True(page.Scroller.IsFocused);
            Assert.Equal("beta gamma", page.Copied());
        });
    }

    [Theory]
    [InlineData("Olá, bão?", 5, "bão")]
    [InlineData("Olá, bão?", 7, "bão")]
    [InlineData("Olá, bão?", 8, "?")]
    [InlineData("a  b", 1, "  ")]
    [InlineData("x.y", 1, ".")]
    public void FindsTheWordACharacterBelongsTo(string text, int character, string word)
    {
        var (start, end) = TextSelection.WordAround(text, character)!.Value;

        Assert.Equal(word, text[start..end]);
    }

    private static (int, int) Span(SelectableTextBlock block) => (block.SelectionStart, block.SelectionEnd);
}
