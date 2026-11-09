using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Apps.Crafter.Presentation.Tests.Fakes;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class PulseTest
{
    private readonly Clock _clock = new();

    private readonly Pulse _pulse;

    public PulseTest() => _pulse = new Pulse(_clock);

    [Fact]
    public void ThinksFirstThenTellsWhatTheAgentIsDoing()
    {
        _pulse.Start();
        Assert.True(_pulse.IsShown);
        Assert.Equal("Thinking…", _pulse.Word);

        _pulse.Heard(new Act("1", ActKind.Command, "dotnet test"));
        Assert.Equal("Working…", _pulse.Word);

        _pulse.Heard(new ActResult("1", true, "Passed"));
        Assert.Equal("Thinking…", _pulse.Word);

        _pulse.Heard(new Speech("Done."));
        Assert.Equal("Writing…", _pulse.Word);

        _pulse.Stop();
    }

    [Fact]
    public void StepsAsideWhileTheAgentWaitsOnYou()
    {
        _pulse.Start();

        _pulse.Heard(new Permission("p", ActKind.Command, "rm -rf bin", null, []));
        Assert.False(_pulse.IsShown);

        _pulse.Resume();
        Assert.True(_pulse.IsShown);

        _pulse.Stop();
    }

    [Fact]
    public void CountsTheTimeSinceTheAgentStarted()
    {
        _pulse.Start();
        _clock.Now += TimeSpan.FromSeconds(62);
        _pulse.Tick();

        Assert.Equal("1m 02s", _pulse.Elapsed);

        _pulse.Stop();
    }

    [Fact]
    public void StaysHiddenOnceTheAgentStopped()
    {
        _pulse.Start();
        _pulse.Stop();

        _pulse.Heard(new Speech("Late."));
        _pulse.Resume();

        Assert.False(_pulse.IsShown);
    }
}
