using System.ComponentModel;

namespace Overworld.Apps.Crafter.Axioms;

public sealed record AddAxiom(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The rule, in one sentence, short and in the imperative, like \"Never add comments\".")]
    string Axiom);
