namespace Overworld.Apps.Crafter.Tests.Fakes;

public sealed class Still : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
}
