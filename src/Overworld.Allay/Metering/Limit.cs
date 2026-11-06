namespace Overworld.Allay.Metering;

public sealed record Limit(string Name, int Percent, DateTimeOffset? Resets = null, bool Close = false);
