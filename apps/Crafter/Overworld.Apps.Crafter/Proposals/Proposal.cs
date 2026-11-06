namespace Overworld.Apps.Crafter.Proposals;

public abstract record Proposal;

public sealed record AxiomProposal(string Axiom) : Proposal;

public sealed record SummaryProposal(string? Current, string Proposed) : Proposal;
