using Avalonia;

namespace Overworld.Desktop.Tests;

public sealed class SidebarTest
{
    private static SidebarSection Section(double height, bool fill = false) =>
        new() { MinHeight = height, Fill = fill };

    private static Sidebar Laid(Size size, params SidebarSection[] sections)
    {
        var sidebar = new Sidebar();

        foreach (var section in sections)
        {
            sidebar.Children.Add(section);
        }

        sidebar.Measure(size);
        sidebar.Arrange(new Rect(size));

        return sidebar;
    }

    [Fact]
    public void StacksSectionsAtTheHeightTheyNeed()
    {
        TestApp.Run(() =>
        {
            var palettes = Section(100);
            var form = Section(200);

            Laid(new Size(260, 600), palettes, form);

            Assert.Equal(new Rect(0, 0, 260, 100), palettes.Bounds);
            Assert.Equal(new Rect(0, 100, 260, 200), form.Bounds);
        });
    }

    [Fact]
    public void GivesTheFillingSectionTheHeightThatIsLeft()
    {
        TestApp.Run(() =>
        {
            var palettes = Section(100, fill: true);
            var form = Section(200);

            Laid(new Size(260, 600), palettes, form);

            Assert.Equal(new Rect(0, 0, 260, 400), palettes.Bounds);
            Assert.Equal(new Rect(0, 400, 260, 200), form.Bounds);
        });
    }

    [Fact]
    public void LetsOnlyTheFirstFillingSectionFill()
    {
        TestApp.Run(() =>
        {
            var first = Section(100, fill: true);
            var second = Section(100, fill: true);

            Laid(new Size(260, 600), first, second);

            Assert.Equal(500, first.Bounds.Height);
            Assert.Equal(100, second.Bounds.Height);
        });
    }

    [Fact]
    public void LeavesNoNegativeHeightWhenTheWindowIsTooShort()
    {
        TestApp.Run(() =>
        {
            var palettes = Section(0, fill: true);
            var form = Section(200);

            Laid(new Size(260, 150), palettes, form);

            Assert.Equal(0, palettes.Bounds.Height);
        });
    }

    [Fact]
    public void SkipsAHiddenFillingSection()
    {
        TestApp.Run(() =>
        {
            var palettes = Section(100, fill: true);
            palettes.IsVisible = false;
            var form = Section(200);

            Laid(new Size(260, 600), palettes, form);

            Assert.Equal(new Rect(0, 0, 260, 200), form.Bounds);
        });
    }

    [Fact]
    public void AdoptsItsSectionsAsChildren()
    {
        TestApp.Run(() =>
        {
            var section = Section(100);
            var sidebar = Laid(new Size(260, 600), section);

            Assert.Same(sidebar, section.Parent);

            sidebar.Children.Remove(section);

            Assert.Null(section.Parent);
        });
    }
}
