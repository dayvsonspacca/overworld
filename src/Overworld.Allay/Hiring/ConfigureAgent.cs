using System.ComponentModel;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Hiring;

public sealed record ConfigureAgent(
    [property: Description("The agent's id, as agents-list gives it, like claude-code.")]
    string Agent,
    [property: Description("How freely it works: auto does what it needs, edits changes files but asks before anything else, plan only plans. Leave it out to keep it.")]
    Mode? Mode = null,
    [property: Description("One of the models agents-list gives for the agent, or default for its own. Leave it out to keep it.")]
    string? Model = null,
    [property: Description("How hard it thinks: default or max. Leave it out to keep it.")]
    Effort? Effort = null);
