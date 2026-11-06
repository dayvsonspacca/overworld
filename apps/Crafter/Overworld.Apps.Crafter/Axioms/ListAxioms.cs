using System.ComponentModel;

namespace Overworld.Apps.Crafter.Axioms;

public sealed record ListAxioms(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder);
