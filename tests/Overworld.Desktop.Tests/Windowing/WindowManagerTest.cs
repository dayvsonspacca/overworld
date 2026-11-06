using Overworld.Desktop.Windowing;

namespace Overworld.Desktop.Tests.Windowing;

public sealed class WindowManagerTest
{
    [Fact]
    public void TilesUnderHyprland()
    {
        var manager = WindowManager.Detect(name => name == "HYPRLAND_INSTANCE_SIGNATURE" ? "a1b2c3" : null);

        Assert.True(manager.Tiles);
    }

    [Fact]
    public void StacksElsewhere()
    {
        var manager = WindowManager.Detect(_ => null);

        Assert.False(manager.Tiles);
    }

    [Fact]
    public void StacksWhenHyprlandLeavesItsSignatureEmpty()
    {
        var manager = WindowManager.Detect(name => name == "HYPRLAND_INSTANCE_SIGNATURE" ? "" : null);

        Assert.False(manager.Tiles);
    }
}
