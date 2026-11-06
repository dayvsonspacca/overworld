namespace Overworld.Apps.Allay.Presentation.Tests.Fakes;

public sealed class Clock(DateTimeOffset now) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => now;
}
