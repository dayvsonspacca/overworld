using System.ComponentModel;

namespace Overworld.Allay.Metering;

public sealed record MeasureAgent(
    [property: Description("The agent's id, as agents-list gives it, like claude-code.")]
    string Agent);
