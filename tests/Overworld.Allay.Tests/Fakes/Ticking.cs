namespace Overworld.Allay.Tests.Fakes;

public sealed class Ticking(TimeSpan step) : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    public override DateTimeOffset GetUtcNow() => _now += step;
}
