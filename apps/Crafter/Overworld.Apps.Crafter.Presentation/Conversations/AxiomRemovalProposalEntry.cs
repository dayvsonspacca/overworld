namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class AxiomRemovalProposalEntry(string axiom, string why) : ProposalEntry
{
    public string Axiom { get; } = axiom;

    public string Why { get; } = why;

    public bool HasWhy => Why.Length > 0;
}
