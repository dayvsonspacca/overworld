using System.ComponentModel;

namespace Overworld.Apps.Crafter.Library;

public sealed record SaveSkill(
    [property: Description("The skill's id: lowercase letters, digits and dashes, like laravel-migrations. An id the library holds is replaced.")]
    string Id,
    [property: Description("The skill's name, as people read it.")]
    string Name,
    [property: Description("What the skill is for, in one sentence.")]
    string Description,
    [property: Description("Instructions for the agent, in Markdown.")]
    string Instructions,
    [property: Description("When an agent should reach for it.")]
    string? WhenToUse = null);
