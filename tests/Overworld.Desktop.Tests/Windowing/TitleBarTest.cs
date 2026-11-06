using Overworld.Desktop.Windowing;

namespace Overworld.Desktop.Tests.Windowing;

public sealed class TitleBarTest
{
    [Fact]
    public void MarksItselfWhenTheWindowManagerTiles()
    {
        TestApp.Run(() =>
        {
            var bar = new TitleBar { Manager = new WindowManager(Tiles: true) };

            Assert.Contains(":tiles", bar.Classes);
        });
    }

    [Fact]
    public void LeavesTheMarkOffWhenTheWindowManagerStacks()
    {
        TestApp.Run(() =>
        {
            var bar = new TitleBar { Manager = new WindowManager(Tiles: false) };

            Assert.DoesNotContain(":tiles", bar.Classes);
        });
    }

    [Fact]
    public void FollowsAChangeOfWindowManager()
    {
        TestApp.Run(() =>
        {
            var bar = new TitleBar { Manager = new WindowManager(Tiles: true) };

            bar.Manager = new WindowManager(Tiles: false);

            Assert.DoesNotContain(":tiles", bar.Classes);
        });
    }

    [Fact]
    public void IsNotMaximizedOutsideAWindow()
    {
        TestApp.Run(() =>
        {
            var bar = new TitleBar();

            Assert.DoesNotContain(":maximized", bar.Classes);
        });
    }
}
