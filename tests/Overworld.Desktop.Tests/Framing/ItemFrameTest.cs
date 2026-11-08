using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Overworld.Desktop.Framing;

namespace Overworld.Desktop.Tests.Framing;

public sealed class ItemFrameTest
{
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(5);

    private static (Window Window, ScrollViewer Scroll, ItemFrame Frame, Border Item) Shown(double above, TimeSpan linger, TimeSpan? wake = null)
    {
        var item = new Border();
        var frame = new ItemFrame { Title = "Amethyst palette", Source = "Painting", StageHeight = 200, Item = item, Linger = linger, Wake = wake ?? TimeSpan.FromMilliseconds(50) };
        var scroll = new ScrollViewer
        {
            Content = new StackPanel { Children = { new Border { Height = above }, frame, new Border { Height = 4000 } } },
        };
        var window = new Window { Width = 600, Height = 400, Content = scroll };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        return (window, scroll, frame, item);
    }

    private static void Scroll(ScrollViewer scroll, double to)
    {
        scroll.Offset = new Vector(0, to);
        scroll.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Wait(TimeSpan time)
    {
        var waiting = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => waiting.Continue = false, time);
        Dispatcher.UIThread.PushFrame(waiting);
    }

    [Fact]
    public void ShowsItsItemWhileSeen()
    {
        TestApp.Run(() =>
        {
            var (window, _, frame, item) = Shown(above: 0, Long);

            Assert.True(frame.IsSeen);
            Assert.True(item.IsAttachedToVisualTree());
            window.Close();
        });
    }

    [Fact]
    public void KeepsItsItemAwayWhileFarOutOfSight()
    {
        TestApp.Run(() =>
        {
            var (window, _, frame, item) = Shown(above: 2000, Long);

            Assert.False(frame.IsSeen);
            Assert.False(item.IsAttachedToVisualTree());
            window.Close();
        });
    }

    [Fact]
    public void ComesAliveAMomentAfterScrollingNearlyIntoSight()
    {
        TestApp.Run(() =>
        {
            var (window, scroll, frame, item) = Shown(above: 2000, Long);

            Scroll(scroll, 2000 - 400 - (ItemFrame.Reach / 2));

            Assert.False(frame.IsSeen);

            Wait(TimeSpan.FromMilliseconds(150));

            Assert.True(frame.IsSeen);
            Assert.True(item.IsAttachedToVisualTree());
            window.Close();
        });
    }

    [Fact]
    public void NeverComesAliveWhenOnlyFlickedPast()
    {
        TestApp.Run(() =>
        {
            var (window, scroll, frame, _) = Shown(above: 2000, Long, wake: TimeSpan.FromMilliseconds(100));

            Scroll(scroll, 1900);
            Scroll(scroll, 3000);
            Wait(TimeSpan.FromMilliseconds(250));

            Assert.False(frame.IsSeen);
            window.Close();
        });
    }

    [Fact]
    public void LingersAMomentOutOfSightThenLetsItsItemGo()
    {
        TestApp.Run(() =>
        {
            var (window, scroll, frame, item) = Shown(above: 0, TimeSpan.FromMilliseconds(100));

            Scroll(scroll, 2000);

            Assert.True(frame.IsSeen);

            Wait(TimeSpan.FromMilliseconds(300));

            Assert.False(frame.IsSeen);
            Assert.False(item.IsAttachedToVisualTree());
            window.Close();
        });
    }

    [Fact]
    public void StaysAliveWhenScrolledBackBeforeLetting()
    {
        TestApp.Run(() =>
        {
            var (window, scroll, frame, item) = Shown(above: 0, TimeSpan.FromMilliseconds(200));

            Scroll(scroll, 2000);
            Scroll(scroll, 0);
            Wait(TimeSpan.FromMilliseconds(350));

            Assert.True(frame.IsSeen);
            Assert.True(item.IsAttachedToVisualTree());
            window.Close();
        });
    }

    [Fact]
    public void LetsItsItemGoWhileWhatHoldsItIsHiddenAndComesBackWhenShown()
    {
        TestApp.Run(() =>
        {
            var (window, scroll, frame, item) = Shown(above: 0, TimeSpan.FromMilliseconds(50));

            scroll.IsVisible = false;
            Dispatcher.UIThread.RunJobs();
            Wait(TimeSpan.FromMilliseconds(150));

            Assert.False(frame.IsSeen);
            Assert.False(item.IsAttachedToVisualTree());

            scroll.IsVisible = true;
            Dispatcher.UIThread.RunJobs();

            Assert.True(frame.IsSeen);
            Assert.True(item.IsAttachedToVisualTree());
            window.Close();
        });
    }

    [Fact]
    public void ScrollsWhatHoldsItWhenTheWheelTurnsOverItsItem()
    {
        TestApp.Run(() =>
        {
            var (window, scroll, frame, item) = Shown(above: 0, Long);
            var held = 0;
            item.PointerWheelChanged += (_, _) => held++;

            window.MouseWheel(new Point(300, 150), new Vector(0, -2));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(100, scroll.Offset.Y);
            Assert.Equal(0, held);
            window.Close();
        });
    }

    [Fact]
    public void ShowsNoExpandButtonWithoutACommand()
    {
        TestApp.Run(() =>
        {
            var (window, _, frame, _) = Shown(above: 0, Long);

            var expand = frame.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_Expand");

            Assert.False(expand.IsVisible);
            window.Close();
        });
    }
}
