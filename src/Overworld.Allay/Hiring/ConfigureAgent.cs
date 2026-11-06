using System.ComponentModel;

namespace Overworld.Allay.Hiring;

public sealed record ConfigureAgent(
    [property: Description("The agent's id, as agents-list gives it, like claude-code.")]
    string Agent,
    [property: Description("The setting's key, as agents-list gives it, like model.")]
    string Key,
    [property: Description("The value to set; leave it out to go back to the default.")]
    string? Value = null);
