using System.ComponentModel;

namespace Overworld.Apps.Crafter.Proposals;

public sealed record ProposeAxiomRemoval(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The axiom to drop, word for word as the project holds it.")]
    string Axiom,
    [property: Description("Why it no longer holds, in one sentence.")]
    string Why);
