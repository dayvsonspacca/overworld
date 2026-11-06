namespace Overworld.Apps.Crafter.Proposals;

public delegate Task<bool> Asker(Proposal proposal, CancellationToken cancellationToken);
