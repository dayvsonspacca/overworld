using System.ComponentModel;

namespace Overworld.Apps.Crafter.Proposals;

public sealed record ProposeAxiom(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The rule, in one sentence, short and in the imperative, like \"Never add comments\".")]
    string Axiom);
