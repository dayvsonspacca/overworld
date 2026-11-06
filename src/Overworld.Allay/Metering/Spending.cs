namespace Overworld.Allay.Metering;

public sealed record Spending(string Name, decimal Spent, string Currency, decimal? Cap = null);
