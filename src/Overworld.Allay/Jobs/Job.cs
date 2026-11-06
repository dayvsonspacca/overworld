namespace Overworld.Allay.Jobs;

public sealed record Job(string Id, JobKind Kind, string Description, bool Prints = false);
