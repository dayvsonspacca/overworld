using System.ComponentModel;

namespace Overworld.Apps.Crafter.Summaries;

public sealed record WriteSummary(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The whole summary, in Markdown. A blank one takes the summary away.")]
    string Text);
