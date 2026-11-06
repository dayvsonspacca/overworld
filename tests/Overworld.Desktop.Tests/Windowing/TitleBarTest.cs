using Avalonia.Controls;
using Overworld.Bedrock.Apps;
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

    [Fact]
    public void LendsItsTitleToTheWindow()
    {
        TestApp.Run(() =>
        {
            TitleBar? bar = null;
            var window = DesktopHost.Window(new AppIdentity("test"), frame =>
            {
                bar = frame.Bar;

                return new Panel();
            });

            bar!.Title = "A chat";
            Assert.Equal("A chat", window.Title);

            bar.Title = null;
            Assert.Equal("Test", window.Title);
        });
    }
}
