using System.ComponentModel;

namespace Overworld.Allay.Hiring;

public sealed record CheckAgent(
    [property: Description("The agent's id, as agents-list gives it, like claude-code.")]
    string Agent);
