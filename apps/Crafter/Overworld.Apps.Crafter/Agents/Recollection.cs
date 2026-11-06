namespace Overworld.Apps.Crafter.Agents;

public sealed record Recollection(IReadOnlyList<PastChat> Latest, int Total);
