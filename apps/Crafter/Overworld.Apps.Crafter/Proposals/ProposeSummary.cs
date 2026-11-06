using System.ComponentModel;

namespace Overworld.Apps.Crafter.Proposals;

public sealed record ProposeSummary(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The whole new summary in Markdown: what the project is, its stack, its layout and the commands it runs with.")]
    string Summary);
