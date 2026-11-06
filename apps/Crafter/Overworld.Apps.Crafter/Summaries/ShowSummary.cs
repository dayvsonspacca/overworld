using System.ComponentModel;

namespace Overworld.Apps.Crafter.Summaries;

public sealed record ShowSummary(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder);
