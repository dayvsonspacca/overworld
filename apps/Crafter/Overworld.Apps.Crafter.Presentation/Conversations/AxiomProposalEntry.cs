namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class AxiomProposalEntry(string axiom) : ProposalEntry
{
    public string Axiom { get; } = axiom;
}
