using Avalonia.Controls;
using Avalonia.Threading;
using Overworld.Bedrock.Apps;

namespace Overworld.Desktop.Tests;

public sealed class LeavingTest
{
    private static Window Shown(Action<Frame> dress)
    {
        var window = DesktopHost.Window(new AppIdentity("test"), frame =>
        {
            dress(frame);

            return new Border();
        });

        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static void Wait(TimeSpan time)
    {
        var waiting = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => waiting.Continue = false, time);
        Dispatcher.UIThread.PushFrame(waiting);
    }

    [Fact]
    public void ClosesAtOnceWithNothingToDoOnLeaving()
    {
        TestApp.Run(() =>
        {
            var closed = false;
            var window = Shown(_ => { });
            window.Closed += (_, _) => closed = true;

            window.Close();

            Assert.True(closed);
        });
    }

    [Fact]
    public void StaysOpenUntilWhatTheAppDoesOnLeavingIsDone()
    {
        TestApp.Run(() =>
        {
            var leaving = new TaskCompletionSource();
            var closed = false;
            var window = Shown(frame => frame.Leaving.Add(_ => leaving.Task));
            window.Closed += (_, _) => closed = true;

            window.Close();
            Wait(TimeSpan.FromMilliseconds(100));

            Assert.False(closed);

            leaving.SetResult();
            Wait(TimeSpan.FromMilliseconds(100));

            Assert.True(closed);
        });
    }

    [Fact]
    public void DoesEverythingTheAppDoesOnLeavingTogether()
    {
        TestApp.Run(() =>
        {
            var first = new TaskCompletionSource();
            var second = new TaskCompletionSource();
            var started = 0;
            var window = Shown(frame =>
            {
                frame.Leaving.Add(_ => { started++; return first.Task; });
                frame.Leaving.Add(_ => { started++; return second.Task; });
            });

            window.Close();
            Wait(TimeSpan.FromMilliseconds(50));

            Assert.Equal(2, started);

            first.SetResult();
            second.SetResult();
            Wait(TimeSpan.FromMilliseconds(100));
        });
    }

    [Fact]
    public void ClosesAnywayOnceItHasWaitedLongEnough()
    {
        TestApp.Run(() =>
        {
            var closed = false;
            var cancelled = false;
            var window = Shown(frame => frame.Leaving.Add(async token =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
            }));
            window.Closed += (_, _) => closed = true;

            window.Close();
            Wait(DesktopHost.Patience + TimeSpan.FromMilliseconds(300));

            Assert.True(closed);
            Assert.True(cancelled);
        });
    }
}
