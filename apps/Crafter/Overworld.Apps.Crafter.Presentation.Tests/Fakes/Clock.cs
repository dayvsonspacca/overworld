namespace Overworld.Apps.Crafter.Presentation.Tests.Fakes;

public sealed class Clock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;

    public override DateTimeOffset GetUtcNow() => Now;
}
