using System.ComponentModel;

namespace Overworld.Allay.Hiring;

public sealed record HireAgent(
    [property: Description("The agent's id, as agents-list gives it, like claude-code.")]
    string Agent,
    [property: Description("False lets the agent go.")]
    bool Hired = true);
