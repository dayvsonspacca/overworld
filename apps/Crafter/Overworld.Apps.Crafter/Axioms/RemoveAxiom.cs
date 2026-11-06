using System.ComponentModel;

namespace Overworld.Apps.Crafter.Axioms;

public sealed record RemoveAxiom(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The axiom to drop, as list-axioms gives it.")]
    string Axiom);
