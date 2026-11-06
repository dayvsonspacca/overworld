namespace Overworld.Apps.Crafter.Proposals;

public abstract record Proposal;

public sealed record AxiomProposal(string Axiom) : Proposal;

public sealed record AxiomRemovalProposal(string Axiom, string Why) : Proposal;

public sealed record SummaryProposal(string? Current, string Proposed) : Proposal;
